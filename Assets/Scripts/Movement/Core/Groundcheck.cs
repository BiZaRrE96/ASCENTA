using UnityEngine;
using System;
using ASCENTA.Events;

public class Groundcheck : MonoBehaviour
{
    [SerializeField, Min(0f)] float checkDistance = 0.3f;
    [SerializeField, Min(0f)] float checkRadius = 0.15f;
    [SerializeField] LayerMask groundMask;
    [SerializeField, Min(0f)] float movingPlatformEffectBlockWindow = 0.1f;
    [SerializeField, Min(0f)] float movingPlatformGroundedGrace = 0.05f;
    [SerializeField, Min(0f)] float movingPlatformGroundedMaxSeparation = 0.12f;
    [SerializeField] bool showGizmos = true;
    [SerializeField] Color groundedGizmoColor = new Color(0f, 1f, 0f, 0.7f);
    [SerializeField] Color ungroundedGizmoColor = new Color(1f, 0f, 0f, 0.7f);

    public bool IsGrounded { get; private set; }
    public bool IsGroundedOnMovingPlatform => IsGrounded && CurrentMovingPlatform != null;
    public bool IsMovingPlatformEffectBlocked => Time.time < movingPlatformEffectBlockUntilTime;
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public Vector3 GroundHitPoint { get; private set; }
    public Collider GroundCollider { get; private set; }
    public MovingPlatform CurrentMovingPlatform { get; private set; }
    public event Action OnLanded;
    public event Action OnUngrounded;
    public event Action<MovingPlatform, Vector3, bool> OnMovingPlatformEntered;

    bool wasGrounded;
    float movingPlatformEffectBlockUntilTime;
    float lastGroundedTime;
    MovingPlatform lastGroundedMovingPlatform;
    Vector3 lastGroundedNormal = Vector3.up;
    Vector3 lastGroundedHitPoint;
    Collider lastGroundedCollider;

    void FixedUpdate()
    {
        Vector3 origin = transform.position;
        Vector3 direction = -transform.up;
        float distance = Mathf.Max(0f, checkDistance);
        float radius = Mathf.Max(0f, checkRadius);
        float sweepDistance = radius > 0f ? distance + radius : distance;

        RaycastHit hit;
        bool groundedNow = radius > 0f
            ? Physics.SphereCast(origin, radius, direction, out hit, sweepDistance, groundMask, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(origin, direction, out hit, distance, groundMask, QueryTriggerInteraction.Ignore);
        Vector3 resolvedNormal = groundedNow ? hit.normal : transform.up;
        Vector3 resolvedHitPoint = groundedNow ? hit.point : transform.position;
        Collider resolvedCollider = groundedNow ? hit.collider : null;
        MovingPlatform nextMovingPlatform = groundedNow ? resolvedCollider.GetComponentInParent<MovingPlatform>() : null;
        bool enteredMovingPlatform = nextMovingPlatform != null && (!wasGrounded || nextMovingPlatform != CurrentMovingPlatform);
        bool movingPlatformEntryWasBlocked = Time.time < movingPlatformEffectBlockUntilTime;
        if (enteredMovingPlatform)
        {
            movingPlatformEffectBlockUntilTime = Time.time + Mathf.Max(0f, movingPlatformEffectBlockWindow);
        }

        if (!groundedNow && wasGrounded && movingPlatformGroundedGrace > 0f)
        {
            bool wasOnMovingPlatform = CurrentMovingPlatform != null;
            bool platformMovingUp = wasOnMovingPlatform && CurrentMovingPlatform.FrameDelta.y > Mathf.Epsilon;
            bool withinGrace = Time.time - lastGroundedTime <= movingPlatformGroundedGrace;
            float verticalSeparation = Mathf.Abs(transform.position.y - lastGroundedHitPoint.y);
            bool withinSeparation = verticalSeparation <= Mathf.Max(0f, movingPlatformGroundedMaxSeparation);
            if (wasOnMovingPlatform && platformMovingUp && withinGrace && withinSeparation)
            {
                groundedNow = true;
                resolvedNormal = lastGroundedNormal;
                resolvedHitPoint = lastGroundedHitPoint;
                resolvedCollider = lastGroundedCollider;
                nextMovingPlatform = lastGroundedMovingPlatform;
            }
        }

        bool groundedChanged = groundedNow != wasGrounded;

        if (groundedNow && !wasGrounded)
        {
            OnLanded?.Invoke();
        }
        else if (!groundedNow && wasGrounded)
        {
            OnUngrounded?.Invoke();
        }

        IsGrounded = groundedNow;
        GroundNormal = groundedNow ? resolvedNormal : transform.up;
        GroundHitPoint = groundedNow ? resolvedHitPoint : transform.position;
        GroundCollider = groundedNow ? resolvedCollider : null;
        CurrentMovingPlatform = nextMovingPlatform;

        if (groundedNow)
        {
            lastGroundedTime = Time.time;
            lastGroundedMovingPlatform = nextMovingPlatform;
            lastGroundedNormal = GroundNormal;
            lastGroundedHitPoint = GroundHitPoint;
            lastGroundedCollider = GroundCollider;
        }

        if (enteredMovingPlatform)
        {
            OnMovingPlatformEntered?.Invoke(nextMovingPlatform, GroundHitPoint, movingPlatformEntryWasBlocked);
        }

        if (groundedChanged)
        {
            EventBus.Publish(new GroundedChangedEvent(IsGrounded, GroundNormal, GroundCollider));
        }

        wasGrounded = groundedNow;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
        {
            return;
        }

        float distance = Mathf.Max(0f, checkDistance);
        float radius = Mathf.Max(0f, checkRadius);
        float sweepDistance = radius > 0f ? distance + radius : distance;

        Vector3 origin = transform.position;
        Vector3 direction = -transform.up;
        Vector3 apex = origin + direction * sweepDistance;
        Color castColor = IsGrounded ? groundedGizmoColor : ungroundedGizmoColor;

        Gizmos.color = castColor;
        Gizmos.DrawLine(origin, apex);
        Gizmos.DrawWireSphere(origin, Mathf.Max(0.02f, radius));
        Gizmos.DrawWireSphere(apex, Mathf.Max(0.02f, radius));

        if (IsGrounded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(apex, apex + GroundNormal * 0.5f);
        }
    }
}
