namespace DiscordPlugin
{
    /// <summary>
    /// What the second line shows while controlling. Values are persisted as numbers in
    /// Discord.json, so don't reorder them.
    /// </summary>
    public enum DisplayType
    {
        None = 0,
        TxSent = 1,
        TxReceived = 2,
        TxBoth = 3,
        AcftSpotted = 4,
        AcftControlled = 5,
    }
}
