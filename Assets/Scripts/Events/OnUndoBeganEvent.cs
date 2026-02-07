namespace ASCENTA.Events
{
    /// <summary>
    /// Fired when an undo begins.
    /// </summary>
    public readonly struct OnUndoBeganEvent : IEvent
    {
        public OnUndoBeganEvent(LastJumpTracker.LastJumpPosition jump, float snapTime)
        {
            Jump = jump;
            SnapTime = snapTime;
        }

        public LastJumpTracker.LastJumpPosition Jump { get; }
        public float SnapTime { get; }
    }
}
