using ArchiSteamFarm.Core;
using ArchiSteamFarm.NLog;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using SteamKit2;
using System.Reflection;

namespace ASFAchievementManagerEx;

internal static class Utils
{
    /// <summary>
    /// 插件配置
    /// </summary>
    internal static PluginConfig Config { get; set; } = new();

    /// <summary>
    /// 更新已就绪
    /// </summary>
    internal static bool UpdatePadding { get; set; }

    /// <summary>
    /// 更新标记
    /// </summary>
    /// <returns></returns>
    private static string UpdateFlag()
    {
        if (UpdatePadding)
        {
            return "*";
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatStaticResponse(string message)
    {
        string flag = UpdateFlag();

        return $"<ASFE{flag}> {message}";
    }

    /// <summary>
    /// 格式化返回文本
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    internal static string FormatBotResponse(this Bot bot, string message)
    {
        string flag = UpdateFlag();

        return $"<{bot.BotName}{flag}> {message}";
    }

    /// <summary>
    /// 获取版本号
    /// </summary>
    internal static Version MyVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0");

    /// <summary>
    /// 获取插件所在路径
    /// </summary>
    internal static string MyLocation => Assembly.GetExecutingAssembly().Location;

    /// <summary>
    /// Steam商店链接
    /// </summary>
    internal static Uri SteamStoreURL => ArchiWebHandler.SteamStoreURL;

    /// <summary>
    /// Steam社区链接
    /// </summary>
    internal static Uri SteamCommunityURL => ArchiWebHandler.SteamCommunityURL;

    /// <summary>
    /// SteamAPI链接
    /// </summary>
    internal static Uri SteamApiURL => new("https://api.steampowered.com");

    /// <summary>
    /// 日志
    /// </summary>
    internal static ArchiLogger ASFLogger => ASF.ArchiLogger;

    /// <summary>
    /// 布尔转换为char
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    internal static char Bool2Str(bool b) => b ? '√' : '×';

    internal static KeyValue? FindByName(this KeyValue keyValue, string name)
    {
        return keyValue?.Children.Find(x => x.Name == name);
    }

    internal static KeyValue? FindByName(this List<KeyValue> keyValues, string name)
    {
        return keyValues.Find(x => x.Name == name);
    }

    internal static IEnumerable<KeyValue> FindEnumByName(this KeyValue keyValue, string name)
    {
        var node = keyValue.FindByName(name);
        return node?.Children ?? Enumerable.Empty<KeyValue>();
    }

    internal static List<KeyValue>? FindListByName(this KeyValue keyValue, string name)
    {
        return keyValue.FindByName(name)?.Children;
    }

    internal static List<KeyValue>? FindListByName(this List<KeyValue> keyValues, string name)
    {
        return keyValues.FindByName(name)?.Children;
    }

    internal static int ReadAsInt(this KeyValue keyValue, string name, int defaultValue = 0)
    {
        var strValue = keyValue.FindByName(name)?.Value;
        return int.TryParse(strValue, out var value) ? value : defaultValue;
    }

    internal static uint ReadAsUInt(this KeyValue keyValue, string name, uint defaultValue = 0)
    {
        var strValue = keyValue.FindByName(name)?.Value;
        return uint.TryParse(strValue, out var value) ? value : defaultValue;
    }
}
