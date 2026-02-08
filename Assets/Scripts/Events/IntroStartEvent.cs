namespace ASCENTA.Events
{
    /// <summary>
    /// Raised when the intro cutscene is about to begin or is skipped.
    /// </summary>
    public readonly struct IntroStartEvent : IEvent
    {
        public IntroStartEvent(bool willPlay)
        {
            WillPlay = willPlay;
        }

        public bool WillPlay { get; }
    }
}
