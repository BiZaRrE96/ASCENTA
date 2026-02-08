namespace ASCENTA.Events
{
    public readonly struct UIShowRequestEvent : IEvent
    {
        public bool UI_ID { get; }
        public bool HideOthers { get; }
    }
}
