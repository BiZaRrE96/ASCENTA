namespace ASCENTA.Events
{
    public readonly struct SettingsChangedEvent : IEvent
    {
        public readonly ConfigData Config;

        public SettingsChangedEvent(ConfigData config)
        {
            Config = config;
        }
    }
}
