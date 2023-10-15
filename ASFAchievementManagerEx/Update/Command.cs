using System.IO.Compression;
using System.Text;

namespace ASFAchievementManagerEx.Update;

internal static class Command
{
    /// <summary>
    /// 查看插件版本
    /// </summary>
    /// <returns></returns>
    internal static string? ResponseASFEnhanceVersion()
    {
        return FormatStaticResponse(string.Format(Langs.PluginVer, nameof(ASFAchievementManagerEx), MyVersion.ToString()));
    }
}
