namespace ASCENTA.Events
{
    /// <summary>
    /// Raised when game data has been loaded. Payload indicates whether a save exists.
    /// </summary>
    public readonly struct GameDataLoadedEvent : IEvent
    {
        public readonly bool hasSaveData;

        public GameDataLoadedEvent(bool hasSaveData)
        {
            this.hasSaveData = hasSaveData;
        }
    }
}
