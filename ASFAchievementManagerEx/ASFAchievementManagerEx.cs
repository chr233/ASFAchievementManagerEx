using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ASFAchievementManagerEx.Core;
using ASFAchievementManagerEx.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;
using System.ComponentModel;
using System.Composition;
using System.Text;

namespace ASFAchievementManagerEx;

[Export(typeof(IPlugin))]
public sealed class ASFAchievementManagerEx : IASF, IBotSteamClient, IBotCommand2
{
    public string Name => "ASF Achievemenevement Manager Ex";
    public Version Version => MyVersion;

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

        Config = config ?? new();

        //使用协议
        if (!Config.EULA)
        {
            sb.AppendLine();
            sb.AppendLine(Static.Line);
            sb.AppendLine(Langs.EulaWarning);
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
        var message = new StringBuilder("\n");
        message.AppendLine(Static.Line);
        message.AppendLine(Static.Logo);
        message.AppendLine(Static.Line);
        message.AppendLine(string.Format(Langs.PluginVer, nameof(ASFAchievementManagerEx), MyVersion.ToString()));
        message.AppendLine(Langs.PluginContact);
        message.AppendLine(Langs.PluginInfo);
        message.AppendLine(Static.Line);

        var pluginFolder = Path.GetDirectoryName(MyLocation) ?? ".";
        var backupPath = Path.Combine(pluginFolder, $"{nameof(ASFAchievementManagerEx)}.bak");

        if (File.Exists(backupPath))
        {
            try
            {
                File.Delete(backupPath);
                message.AppendLine(Langs.CleanUpOldBackup);
            }
            catch (Exception e)
            {
                ASFLogger.LogGenericException(e);
                message.AppendLine(Langs.CleanUpOldBackupFailed);
            }
        }
        else
        {
            message.AppendLine(Langs.ASFEVersionTips);
            message.AppendLine(Langs.ASFEUpdateTips);
        }

        message.AppendLine(Static.Line);

        ASFLogger.LogGenericInfo(message.ToString());

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理命令
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="access"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <param name="steamId"></param>
    /// <returns></returns>
    private static Task<string?>? ResponseCommand(Bot bot, EAccess access, string message, string[] args)
    {
        var cmd = args[0].ToUpperInvariant();

        if (cmd.StartsWith("AAM."))
        {
            cmd = cmd[5..];
        }
        else
        {
            //跳过禁用命令
            if (Config.DisabledCmds?.Contains(cmd) == true)
            {
                ASFLogger.LogGenericInfo("Command {0} is disabled!");
                return null;
            }
        }

        var argLength = args.Length;
        return argLength switch
        {
            0 => throw new InvalidOperationException(nameof(args)),
            1 => null,
            _ => cmd switch
            {
                "ALIST" when argLength > 2 && access >= EAccess.Master =>
                    Command.ResponseAchievementList(args[1], Utilities.GetArgsAsText(args, 2, ",")),
                "ALIST" when access >= EAccess.Master =>
                    Command.ResponseAchievementList(bot, args[1]),

                "ASTATS" when argLength > 2 && access >= EAccess.Master =>
                    Command.ResponseAchievementStatList(args[1], Utilities.GetArgsAsText(args, 2, ",")),
                "ASTATS" when argLength > 1 && access >= EAccess.Master =>
                    Command.ResponseAchievementStatList(bot, args[1]),

                "AUNLOCK" when argLength > 3 && access >= EAccess.Master =>
                     Command.ResponseAchievementSet(args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), true),
                "AUNLOCK" when argLength > 2 && access >= EAccess.Master =>
                    Command.ResponseAchievementSet(bot, args[1], Utilities.GetArgsAsText(args, 2, ","), true),

                "ALOCK" when argLength > 3 && access >= EAccess.Master =>
                    Command.ResponseAchievementSet(args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), false),
                "ALOCK" when argLength > 2 && access >= EAccess.Master =>
                    Command.ResponseAchievementSet(bot, args[1], Utilities.GetArgsAsText(args, 2, ","), false),

                _ => null,
            },
        };
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
        if (!Enum.IsDefined(access))
        {
            throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(EAccess));
        }

        try
        {
            var task = ResponseCommand(bot, access, message, args);
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
            var version = await bot.Commands.Response(EAccess.Owner, "VERSION") ?? Langs.AccountSubUnknown;
            var i = version.LastIndexOf('V');
            if (i >= 0)
            {
                version = version[++i..];
            }
            var cfg = JsonConvert.SerializeObject(Config, Formatting.Indented);

            var sb = new StringBuilder();
            sb.AppendLine(Langs.ErrorLogTitle);
            sb.AppendLine(Static.Line);
            sb.AppendLine(string.Format(Langs.ErrorLogOriginMessage, message));
            sb.AppendLine(string.Format(Langs.ErrorLogAccess, access.ToString()));
            sb.AppendLine(string.Format(Langs.ErrorLogASFVersion, version));
            sb.AppendLine(string.Format(Langs.ErrorLogPluginVersion, MyVersion));
            sb.AppendLine(Static.Line);
            sb.AppendLine(cfg);
            sb.AppendLine(Static.Line);
            sb.AppendLine(string.Format(Langs.ErrorLogErrorName, ex.GetType()));
            sb.AppendLine(string.Format(Langs.ErrorLogErrorMessage, ex.Message));
            sb.AppendLine(ex.StackTrace);

            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                sb.Insert(0, '\n');
                ASFLogger.LogGenericError(sb.ToString());
            });

            return sb.ToString();
        }
    }
}
