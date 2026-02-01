using UnityEngine;
using System;

public class Groundcheck : MonoBehaviour
{
    [SerializeField] float checkDistance = 0.3f;

    [SerializeField] LayerMask groundMask;

    public bool IsGrounded { get; private set; }
    public event Action OnLanded;
    public event Action OnUngrounded;

    bool wasGrounded;

    void FixedUpdate()
    {
        Transform originTransform = transform.parent != null ? transform.parent : transform;
        Vector3 origin = originTransform.position;
        Vector3 direction = -originTransform.up;
        float distance = Mathf.Max(0f, checkDistance);

        bool groundedNow = Physics.Raycast(origin, direction, distance, groundMask, QueryTriggerInteraction.Ignore);

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
    }
}
