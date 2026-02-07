namespace ASCENTA.Events
{
    /// <summary>
    /// Fired when time reversal begins or ends.
    /// </summary>
    public readonly struct ReversalEvent : IEvent
    {
        public readonly bool IsReversing;
        public readonly float ReversalFixedDelta;

        public ReversalEvent(bool isReversing, float reversalFixedDelta)
        {
            IsReversing = isReversing;
            ReversalFixedDelta = reversalFixedDelta;
        }
    }
}
