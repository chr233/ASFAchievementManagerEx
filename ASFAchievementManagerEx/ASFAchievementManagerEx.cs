using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ASFAchievementManagerEx.Core;
using Newtonsoft.Json.Linq;
using SteamKit2;
using System.ComponentModel;
using System.Composition;
using System.Reflection;
using System.Text;

namespace ASFAchievementManagerEx;

[Export(typeof(IPlugin))]
internal sealed class ASFAchievementManagerEx : IASF, IBotSteamClient, IBotCommand2
{
    public string Name => "ASF Achievemenevement Manager Ex";
    public Version Version => MyVersion;

    private bool ASFEBridge;

    public static PluginConfig Config => Utils.Config;

    private static Timer? StatisticTimer { get; set; }

    /// <summary>
    /// ASF启动事件
    /// </summary>
    /// <param name="additionalConfigProperties"></param>
    /// <returns></returns>
    public Task OnASFInit(IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null)
    {
        PluginConfig? config = null;

        if (additionalConfigProperties != null)
        {
            foreach ((string configProperty, JToken configValue) in additionalConfigProperties)
            {
                if (configProperty == "ASFEnhance" && configValue.Type == JTokenType.Object)
                {
                    try
                    {
                        config = configValue.ToObject<PluginConfig>();
                        if (config != null)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ASFLogger.LogGenericException(ex);
                    }
                }
            }
        }

        Utils.Config = config ?? new();

        var warnings = new StringBuilder();

        //使用协议
        if (!Config.EULA)
        {
            warnings.AppendLine();
            warnings.AppendLine(Langs.Line);
            warnings.AppendLineFormat(Langs.EulaWarning, Name);
            warnings.AppendLine(Langs.Line);
        }

        if (warnings.Length > 0)
        {
            ASFLogger.LogGenericWarning(warnings.ToString());
        }
        //统计
        if (Config.Statistic && !ASFEBridge)
        {
            var request = new Uri("https://asfe.chrxw.com/asfachievemenevementmanagerex");
            StatisticTimer = new Timer(
                async (_) => await ASF.WebBrowser!.UrlGetToHtmlDocument(request).ConfigureAwait(false),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromHours(24)
            );
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 插件加载事件
    /// </summary>
    /// <returns></returns>
    public Task OnLoaded()
    {
        ASFLogger.LogGenericInfo(Langs.PluginContact);
        ASFLogger.LogGenericInfo(Langs.PluginInfo);

        var flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var handler = typeof(ASFAchievementManagerEx).GetMethod(nameof(ResponseCommand), flag);

        const string pluginId = nameof(ASFAchievementManagerEx);
        const string cmdPrefix = "AAM";
        const string repoName = "ASFAchievementManagerEx";

        ASFEBridge = AdapterBridge.InitAdapter(Name, pluginId, cmdPrefix, repoName, handler);

        if (ASFEBridge)
        {
            ASFLogger.LogGenericDebug(Langs.ASFEnhanceRegisterSuccess);
        }
        else
        {
            ASFLogger.LogGenericInfo(Langs.ASFEnhanceRegisterFailed);
            ASFLogger.LogGenericWarning(Langs.PluginStandalongMode);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取插件信息
    /// </summary>
    private static string? PluginInfo => string.Format("{0} {1}", nameof(ASFAchievementManagerEx), MyVersion);

    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="cmd"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    private static Task<string?>? ResponseCommand(Bot bot, EAccess access, string cmd, string[] args)
    {
        var argLength = args.Length;
        return argLength switch
        {
            0 => throw new InvalidOperationException(nameof(args)),
            1 => cmd switch
            {
                //Plugin Info
                "ASFACHIEVEMENTMANAGER" or
                "AAM" when access >= EAccess.FamilySharing =>
                    Task.FromResult(PluginInfo),

                _ => null,
            },
            _ => cmd switch
            {
                "ALIST" when argLength > 2 && access >= EAccess.Operator =>
                    Command.ResponseGetAchievementList(args[1], Utilities.GetArgsAsText(args, 2, ",")),
                "ALIST" when access >= EAccess.Operator =>
                    Command.ResponseGetAchievementList(bot, args[1]),

                "ASTATS" when argLength > 2 && access >= EAccess.Operator =>
                    Command.ResponseGetStatsList(args[1], Utilities.GetArgsAsText(args, 2, ",")),
                "ASTATS" when argLength > 1 && access >= EAccess.Operator =>
                    Command.ResponseGetStatsList(bot, args[1]),

                "ASET" or
                "AUNLOCK" when argLength > 3 && access >= EAccess.Master =>
                     Command.ResponseSetAchievement(args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), true),
                "ASET" or
                "AUNLOCK" when argLength > 2 && access >= EAccess.Master =>
                    Command.ResponseSetAchievement(bot, args[1], Utilities.GetArgsAsText(args, 2, ","), true),

                "ARESET" or
                "ALOCK" when argLength > 3 && access >= EAccess.Master =>
                    Command.ResponseSetAchievement(args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), false),
                "ARESET" or
                "ALOCK" when argLength > 2 && access >= EAccess.Master =>
                    Command.ResponseSetAchievement(bot, args[1], Utilities.GetArgsAsText(args, 2, ","), false),

                "AEDIT" when argLength > 3 && access >= EAccess.Master =>
                    Command.ResponseSetStats(args[1], args[2], Utilities.GetArgsAsText(args, 3, ",")),
                "AEDIT" when argLength > 2 && access >= EAccess.Master =>
                    Command.ResponseSetStats(bot, args[1], Utilities.GetArgsAsText(args, 2, ",")),

                _ => null,
            },
        };
    }

    /// <summary>
    /// 处理命令事件
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <param name="steamId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamId = 0)
    {
        if (ASFEBridge)
        {
            return null;
        }

        if (!Enum.IsDefined(access))
        {
            throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
        }

        try
        {
            var cmd = args[0].ToUpperInvariant();

            if (cmd.StartsWith("AAM."))
            {
                cmd = cmd[4..];
            }

            var task = ResponseCommand(bot, access, cmd, args);
            if (task != null)
            {
                return await task.ConfigureAwait(false);
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                ASFLogger.LogGenericException(ex);
            }).ConfigureAwait(false);

            return ex.StackTrace;
        }
    }

    /// <summary>
    /// 初始化机回调接收器
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="callbackManager"></param>
    /// <returns></returns>
    public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 初始化客户端消息处理器
    /// </summary>
    /// <param name="bot"></param>
    /// <returns></returns>
    public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot)
    {
        var botHandler = new AchievementHandler();
        Command.Handlers.TryAdd(bot, botHandler);
        var handlers = new ClientMsgHandler[] { botHandler };
        return Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>(handlers);
    }
}
