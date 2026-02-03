using UnityEngine;
using System;

public class Groundcheck : MonoBehaviour
{
    [SerializeField] float checkDistance = 0.3f;
    [SerializeField, Min(0f)] float checkRadius = 0.15f;
    [SerializeField] LayerMask groundMask;

    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public Collider GroundCollider { get; private set; }
    public event Action OnLanded;
    public event Action OnUngrounded;

    bool wasGrounded;

    void FixedUpdate()
    {
        Transform originTransform = transform.parent != null ? transform.parent : transform;
        Vector3 origin = originTransform.position;
        Vector3 direction = -originTransform.up;
        float distance = Mathf.Max(0f, checkDistance);

        RaycastHit hit;
        bool groundedNow = false;

        if (checkRadius > 0f)
        {
            Vector3 sphereOrigin = origin - direction * checkRadius;
            groundedNow = Physics.SphereCast(sphereOrigin, checkRadius, direction, out hit, distance, groundMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            groundedNow = Physics.Raycast(origin, direction, out hit, distance, groundMask, QueryTriggerInteraction.Ignore);
        }

        if (groundedNow && !wasGrounded)
        {
            OnLanded?.Invoke();
        }
        else if (!groundedNow && wasGrounded)
        {
            OnUngrounded?.Invoke();
        }

        IsGrounded = groundedNow;
        wasGrounded = groundedNow;

        GroundNormal = groundedNow ? hit.normal : originTransform.up;
        GroundCollider = groundedNow ? hit.collider : null;
    }
}
