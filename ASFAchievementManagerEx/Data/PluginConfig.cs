namespace ASFAchievementManagerEx.Data;
public sealed record PluginConfig
{
    public bool EULA { get; set; } = true;
    public bool Statistic { get; set; } = true;

    public List<string>? DisabledCmds { get; set; }
}
