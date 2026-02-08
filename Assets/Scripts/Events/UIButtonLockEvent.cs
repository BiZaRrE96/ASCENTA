namespace ASCENTA.Events
{
    /// <summary>
    /// Fired to lock or unlock UI buttons.
    /// </summary>
    public readonly struct UIButtonLockEvent : IEvent
    {
        public readonly bool IsLocked;

        public UIButtonLockEvent(bool isLocked)
        {
            IsLocked = isLocked;
        }
    }
}
