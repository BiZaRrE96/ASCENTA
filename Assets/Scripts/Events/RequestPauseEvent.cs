namespace ASCENTA.Events
{
    /// <summary>
    /// Raised when UI or input requests the pause state to change.
    /// </summary>
    public readonly struct RequestPauseEvent : IEvent
    {
        public bool ShouldPause { get; }

        public RequestPauseEvent(bool shouldPause)
        {
            ShouldPause = shouldPause;
        }
    }
}
