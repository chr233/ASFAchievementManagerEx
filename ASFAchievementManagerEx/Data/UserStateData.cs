namespace ASFAchievementManagerEx.Data;


internal sealed record UserStateData
{
    public List<AchievementData>? Achievements { get; set; }
    public List<StatusData>? Stats { get; set; }
}

internal sealed record AchievementData
{
    public uint StatNum { get; set; }
    public int BitNum { get; set; }
    public bool IsSet { get; set; }
    public bool Restricted { get; set; }
    public uint Dependancy { get; set; }
    public uint DependancyValue { get; set; }
    public string? DependancyName { get; set; }
    public string? Name { get; set; }
    public uint StatValue { get; set; }
}

internal record StatusData
{
    public uint Value { get; set; }

    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsIncrementOnly { get; set; }
    public int Permission { get; set; }

    public string Extra
    {
        get
        {
            var flags = StatFlags.None;
            flags |= IsIncrementOnly == false ? 0 : StatFlags.IncrementOnly;
            flags |= (Permission & 2) != 0 == false ? 0 : StatFlags.Protected;
            flags |= (Permission & ~2) != 0 == false ? 0 : StatFlags.UnknownPermission;
            return flags.ToString();
        }
    }
}