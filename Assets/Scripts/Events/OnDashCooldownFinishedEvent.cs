namespace ASCENTA.Events
{
    /// <summary>
    /// Fired by DashController when the dash cooldown finishes.
    /// </summary>
    public readonly struct OnDashCooldownFinishedEvent : IEvent
    {
        public OnDashCooldownFinishedEvent(float readyTime, float cooldownDuration)
        {
            ReadyTime = readyTime;
            CooldownDuration = cooldownDuration;
        }

        public float ReadyTime { get; }
        public float CooldownDuration { get; }
    }
}
