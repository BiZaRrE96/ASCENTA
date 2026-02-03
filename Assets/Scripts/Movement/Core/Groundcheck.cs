using UnityEngine;
using System;
using ASCENTA.Events;

public class Groundcheck : MonoBehaviour
{
    [SerializeField, Min(0f)] float checkDistance = 0.3f;
    [SerializeField, Min(0f)] float checkRadius = 0.15f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] bool showGizmos = true;
    [SerializeField] Color groundedGizmoColor = new Color(0f, 1f, 0f, 0.7f);
    [SerializeField] Color ungroundedGizmoColor = new Color(1f, 0f, 0f, 0.7f);

    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public Collider GroundCollider { get; private set; }
    public event Action OnLanded;
    public event Action OnUngrounded;

    bool wasGrounded;

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
        GroundNormal = groundedNow ? hit.normal : transform.up;
        GroundCollider = groundedNow ? hit.collider : null;

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
