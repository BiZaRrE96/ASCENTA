using UnityEngine;

namespace ASCENTA.Events
{
    /// <summary>
    /// Fired when a jump pad successfully boosts a rigidbody.
    /// </summary>
    public readonly struct OnJumpPadBoostEvent : IEvent
    {
        public OnJumpPadBoostEvent(Vector3 contactPoint, Vector3 direction, float force)
        {
            ContactPoint = contactPoint;
            Direction = direction;
            Force = force;
        }

        public Vector3 ContactPoint { get; }
        public Vector3 Direction { get; }
        public float Force { get; }
    }
}
