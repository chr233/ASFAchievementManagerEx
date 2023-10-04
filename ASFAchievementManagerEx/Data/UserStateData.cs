using System.Security;

namespace ASFAchievementManagerEx.Data;


internal sealed record UserStateData
{
    public IList<AchievementData>? Achievements { get; set; }
    public IList<StatsData>? Stats { get; set; }
}

/// <summary>
/// 成就数据
/// </summary>
internal sealed record AchievementData
{
    public uint StatNum { get; set; }
    public int BitNum { get; set; }
    public bool IsUnlock { get; set; }
    public bool Restricted { get; set; }
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
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// 只允许递增
    /// </summary>
    public bool IsIncrementOnly { get; set; }
    public bool IsProtected => (Permission & 2) != 0;

    public int Permission { get; set; }
    public uint Value { get; set; }
    public int Default { get; set; }
    public int MaxChange { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}