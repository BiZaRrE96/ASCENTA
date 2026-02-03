using UnityEngine;

namespace ASCENTA.Events
{
    /// <summary>
    /// Fired by DashController when a dash is successfully executed.
    /// </summary>
    public readonly struct OnDashEvent : IEvent
    {
        public OnDashEvent(Vector3 direction, float strength, float duration)
        {
            Direction = direction;
            Strength = strength;
            Duration = duration;
        }

        public Vector3 Direction { get; }
        public float Strength { get; }
        public float Duration { get; }
    }
}
