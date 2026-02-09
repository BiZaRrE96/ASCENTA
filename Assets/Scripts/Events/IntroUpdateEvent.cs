namespace ASCENTA.Events
{
    /// <summary>
    /// Raised when the intro cutscene transitions to gameplay.
    /// Regardless if intro was played or not
    /// If intro end is required, see IntroCompletedEvent
    /// </summary>
    public readonly struct IntroUpdateEvent : IEvent
    {
    }
}
