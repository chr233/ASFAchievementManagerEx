namespace ASFAchievementManagerEx.Data;

internal sealed record UserStatsData
{
    public IList<AchievementData>? Achievements { get; set; }
    public IDictionary<uint, StatsData>? Stats { get; set; }
}

/// <summary>
/// 成就数据
/// </summary>
internal sealed record AchievementData
{
    public uint StatId { get; set; }
    public int Bit { get; set; }
    public bool IsUnlock { get; set; }
    public int Permission { get; set; }
    public bool IsProtected => (Permission & 2) != 0;
    public uint Dependancy { get; set; }
    public uint DependancyValue { get; set; }
    public string? DependancyName { get; set; }
    public string? Name { get; set; }
    public uint StatValue { get; set; }
}

/// <summary>
/// 统计数据
/// </summary>
internal record StatsData
{
    public uint Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// 只允许递增
    /// </summary>
    public bool IsIncrementOnly { get; set; }
    public bool IsProtected => (Permission & 2) != 0;

    public int Permission { get; set; }
    public uint Value { get; set; }
    public uint? Default { get; set; }
    public uint? MaxChange { get; set; }
    public uint? Min { get; set; }
    public uint? Max { get; set; }

    public string StrValue => string.Format("{0} {1} {2}", Min != null ? Min.ToString() : "-", Value, Max != null ? Max.ToString() : "-");
}