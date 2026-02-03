using UnityEngine;

namespace ASCENTA.Events
{
    /// <summary>
    /// Fired when grounded state changes.
    /// </summary>
    public readonly struct GroundedChangedEvent : IEvent
    {
        public GroundedChangedEvent(bool isGrounded, Vector3 groundNormal, Collider groundCollider)
        {
            IsGrounded = isGrounded;
            GroundNormal = groundNormal;
            GroundCollider = groundCollider;
        }

        public bool IsGrounded { get; }
        public Vector3 GroundNormal { get; }
        public Collider GroundCollider { get; }
    }
}
