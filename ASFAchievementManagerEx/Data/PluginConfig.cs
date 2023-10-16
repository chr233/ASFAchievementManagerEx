using Newtonsoft.Json;

namespace ASFAchievementManagerEx.Data;
/// <summary>
/// 插件设置
/// </summary>
public sealed record PluginConfig
{
    /// <summary>
    /// 是否同意使用协议
    /// </summary>
    [JsonProperty(Required = Required.DisallowNull)]
    public bool EULA { get; set; }

    /// <summary>
    /// 是否启用统计
    /// </summary>
    [JsonProperty(Required = Required.DisallowNull)]
    public bool Statistic { get; set; } = true;
}
