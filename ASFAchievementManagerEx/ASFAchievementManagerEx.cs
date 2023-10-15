using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ASFAchievementManagerEx.Core;
using Newtonsoft.Json;
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

    private AdapterBtidge? ASFEBridge = null;

    [JsonProperty]
    public static PluginConfig Config => Utils.Config;

    private static Timer? StatisticTimer;

    /// <summary>
    /// ASF启动事件
    /// </summary>
    /// <param name="additionalConfigProperties"></param>
    /// <returns></returns>
    public Task OnASFInit(IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null)
    {
        var sb = new StringBuilder();

        PluginConfig? config = null;

        if (additionalConfigProperties != null)
        {
            foreach ((string configProperty, JToken configValue) in additionalConfigProperties)
            {
                if (configProperty == "ASFAchievementManagerEx" && configValue.Type == JTokenType.Object)
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

        //使用协议
        if (!Config.EULA)
        {
            sb.AppendLine();
            sb.AppendLine(Static.Line);
            sb.AppendLineFormat(Langs.EulaWarning, nameof(ASFAchievementManagerEx));
            sb.AppendLine(Static.Line);
        }

        if (sb.Length > 0)
        {
            ASFLogger.LogGenericWarning(sb.ToString());
        }
        //统计
        if (Config.Statistic)
        {
            var request = new Uri("https://asfe.chrxw.com/asfachievemenevementmanagerex");
            StatisticTimer = new Timer(
                async (_) => await ASF.WebBrowser!.UrlGetToHtmlDocument(request),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromHours(24)
            );
        }
        //禁用命令
        if (Config.DisabledCmds == null)
        {
            Config.DisabledCmds = new();
        }
        else
        {
            for (int i = 0; i < Config.DisabledCmds.Count; i++)
            {
                Config.DisabledCmds[i] = Config.DisabledCmds[i].ToUpperInvariant();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 插件加载事件
    /// </summary>
    /// <returns></returns>
    public Task OnLoaded()
    {
        try
        {
            var flag = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var handler = typeof(ASFAchievementManagerEx).GetMethod(nameof(ResponseCommand), flag);

            const string pluginName = nameof(ASFAchievementManagerEx);
            const string cmdPrefix = "AAM";
            const string repoName = "ASFAchievementManagerEx";

            ASFEBridge = AdapterBtidge.InitAdapter(pluginName, cmdPrefix, repoName, handler);
            ASF.ArchiLogger.LogGenericDebug(ASFEBridge != null ? "ASFEBridge 注册成功" : "ASFEBridge 注册失败");
        }
        catch (Exception ex)
        {
            ASF.ArchiLogger.LogGenericDebug("ASFEBridge 注册出错");
            ASF.ArchiLogger.LogGenericException(ex);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
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
                //Update
                "ASFACHIEVEMENTMANAGER" or
                "AAM" when access >= EAccess.FamilySharing =>
                    Task.FromResult(Update.Command.ResponseASFEnhanceVersion()),

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
        if (ASFEBridge != null)
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

            if (cmd.StartsWith("DEMO."))
            {
                cmd = cmd[5..];
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
                Utils.ASFLogger.LogGenericException(ex);
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
