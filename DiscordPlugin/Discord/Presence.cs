namespace DiscordPlugin.Discord
{
    /// <summary>What to show in Discord. A null presence means "show nothing".</summary>
    internal sealed class Presence
    {
        public Presence(string details, string state, long startUnixSeconds)
        {
            Details = details;
            State = state;
            StartUnixSeconds = startUnixSeconds;
        }

        /// <summary>First line, e.g. "Controlling Melbourne Centre."</summary>
        public string Details { get; }

        /// <summary>Second line, e.g. "12 aircraft spotted."</summary>
        public string State { get; }

        public long StartUnixSeconds { get; }
    }
}
