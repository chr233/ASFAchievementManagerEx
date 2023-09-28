namespace ASFAchievementManagerEx.Data;

internal record IntStatInfo
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
            flags |= ((Permission & 2) != 0) == false ? 0 : StatFlags.Protected;
            flags |= ((Permission & ~2) != 0) == false ? 0 : StatFlags.UnknownPermission;
            return flags.ToString();
        }
    }

}


[Flags]
internal enum StatFlags
{
    None = 0,
    IncrementOnly = 1 << 0,
    Protected = 1 << 1,
    UnknownPermission = 1 << 2,
}