using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Localization;
using SteamKit2;
using System.Linq;
using System.Collections.Concurrent;
using ASFAchievementManagerEx.Core;
using Newtonsoft.Json.Linq;
using System.Text;
using ASFAchievementManagerEx.Data;
using ASFAchievementManagerEx.Localization;

namespace ASFAchievementManagerEx;

[Export(typeof(IPlugin))]
public sealed class ASFAchievemenevementManagerEx : IASF, IBotSteamClient, IBotCommand2
{
    public string Name => "ASF Achievemenevement Manager Ex";

    public Version Version => MyVersion;

    private static readonly ConcurrentDictionary<Bot, AchievementHandler> AchievementHandlers = new();

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

        Utils.Config = config ?? new();

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
                async (_) => await ASF.WebBrowser!.UrlGetToHtmlDocument(request).ConfigureAwait(false),
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


    public Task OnLoaded()
    {
        ASFLogger.LogGenericInfo("ASF Achievement Manager Plugin by Ryzhehvost, powered by ginger cats");
        return Task.CompletedTask;
    }

    public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0)
    {
        switch (args.Length)
        {
            case 0:
                bot.ArchiLogger.LogNullError(null, nameof(args));

                return null;
            case 1:
                return args[0].ToUpperInvariant() switch
                {
                    _ => null,
                };
            default:
                return args[0].ToUpperInvariant() switch
                {
                    "ALIST" when args.Length > 2 => await ResponseAchievementList(access, steamID, args[1], Utilities.GetArgsAsText(args, 2, ",")).ConfigureAwait(false),
                    "ALIST" => await ResponseAchievementList(access, bot, args[1]).ConfigureAwait(false),
                    "ASET" when args.Length > 3 => await ResponseAchievementSet(access, steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), true).ConfigureAwait(false),
                    "ASET" when args.Length > 2 => await ResponseAchievementSet(access, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), true).ConfigureAwait(false),
                    "ARESET" when args.Length > 3 => await ResponseAchievementSet(access, steamID, args[1], args[2], Utilities.GetArgsAsText(args, 3, ","), false).ConfigureAwait(false),
                    "ARESET" when args.Length > 2 => await ResponseAchievementSet(access, bot, args[1], Utilities.GetArgsAsText(args, 2, ","), false).ConfigureAwait(false),
                    _ => null,
                };
        }
    }

    public Task OnBotSteamCallbacksInit(Bot bot, CallbackManager callbackManager) => Task.CompletedTask;

    public Task<IReadOnlyCollection<ClientMsgHandler>?> OnBotSteamHandlersInit(Bot bot)
    {
        AchievementHandler CurrentBotAchievementHandler = new();
        AchievementHandlers.TryAdd(bot, CurrentBotAchievementHandler);
        return Task.FromResult<IReadOnlyCollection<ClientMsgHandler>?>(new HashSet<ClientMsgHandler> { CurrentBotAchievementHandler });
    }

    //Responses

    private static async Task<string?> ResponseAchievementList(EAccess access, Bot bot, string appids)
    {
        if (access < EAccess.Master)
        {
            return null;
        }

        string[] gameIDs = appids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (gameIDs.Length == 0)
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
        }
        if (AchievementHandlers.TryGetValue(bot, out AchievementHandler? AchievementHandler))
        {
            if (AchievementHandler == null)
            {
                bot.ArchiLogger.LogNullError(AchievementHandler);
                return null;
            }

            HashSet<uint> gamesToGetAchievements = new();

            foreach (string game in gameIDs)
            {
                if (!uint.TryParse(game, out uint gameID) || (gameID == 0))
                {
                    return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorParsingObject, nameof(gameID)));
                }

                gamesToGetAchievements.Add(gameID);
            }


            IList<string> results = await Utilities.InParallel(gamesToGetAchievements.Select(appID => Task.Run<string>(() => AchievementHandler.GetAchievements(bot, appID)))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

            return responses.Count > 0 ? bot.Commands.FormatBotResponse(string.Join(Environment.NewLine, responses)) : null;

        }
        else
        {

            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(AchievementHandlers)));
        }

    }

    private static async Task<string?> ResponseAchievementList(EAccess access, ulong steamID, string botNames, string appids)
    {

        HashSet<Bot>? bots = Bot.GetBots(botNames);

        if ((bots == null) || (bots.Count == 0))
        {
            return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementList(Commands.GetProxyAccess(bot, access, steamID), bot, appids))).ConfigureAwait(false);

        List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }


    private static async Task<string?> ResponseAchievementSet(EAccess access, Bot bot, string appid, string achievementNumbers, bool set = true)
    {
        if (access < EAccess.Master)
        {
            return null;
        }

        if (string.IsNullOrEmpty(achievementNumbers))
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorObjectIsNull, nameof(achievementNumbers)));
        }
        if (!uint.TryParse(appid, out uint appId))
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, nameof(appId)));
        }

        if (!AchievementHandlers.TryGetValue(bot, out AchievementHandler? AchievementHandler))
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(AchievementHandlers)));
        }

        if (AchievementHandler == null)
        {
            bot.ArchiLogger.LogNullError(AchievementHandler);
            return null;
        }

        HashSet<uint> achievements = new();

        string[] achievementStrings = achievementNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (!achievementNumbers.Equals("*"))
        {
            foreach (string achievement in achievementStrings)
            {
                if (!uint.TryParse(achievement, out uint achievementNumber) || (achievementNumber == 0))
                {
                    return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorParsingObject, achievement));
                }

                achievements.Add(achievementNumber);
            }
            if (achievements.Count == 0)
            {
                return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, "Achievements list"));
            }
        }
        return bot.Commands.FormatBotResponse(await Task.Run<string>(() => AchievementHandler.SetAchievements(bot, appId, achievements, set)).ConfigureAwait(false));
    }

    private static async Task<string?> ResponseAchievementSet(EAccess access, ulong steamID, string botNames, string appid, string achievementNumbers, bool set = true)
    {

        HashSet<Bot>? bots = Bot.GetBots(botNames);

        if ((bots == null) || (bots.Count == 0))
        {
            return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementSet(Commands.GetProxyAccess(bot, access, steamID), bot, appid, achievementNumbers, set))).ConfigureAwait(false);

        List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

}
