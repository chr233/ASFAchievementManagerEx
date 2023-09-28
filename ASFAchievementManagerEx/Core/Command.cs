using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using ASFAchievementManagerEx.Localization;
using System.Collections.Concurrent;
using System.Text;

namespace ASFAchievementManagerEx.Core;
internal static class Command
{
    internal static ConcurrentDictionary<Bot, AchievementHandler> Handlers { get; private set; } = new();

    /// <summary>
    /// 显示成就列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appids"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementList(Bot bot, string appids)
    {
        if (!Handlers.TryGetValue(bot, out var AchievementHandler))
        {
            ASFLogger.LogNullError(AchievementHandler);
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));

        string[] gameIDs = appids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string game in gameIDs)
        {
            if (!uint.TryParse(game, out uint gameId) || (gameId == 0))
            {
                sb.AppendLine(bot.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, nameof(gameId))));
            }
            else
            {
                var result = await AchievementHandler.GetAchievements(bot, gameId).ConfigureAwait(false);
                sb.AppendLine(bot.FormatBotResponse(result));
            }

            sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    /// 获取成就列表 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appids"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementList(string botNames, string appids)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementList(bot, appids))).ConfigureAwait(false);

        List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 修改成就
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appid"></param>
    /// <param name="achievementNumbers"></param>
    /// <param name="set"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementSet(Bot bot, string appid, string achievementNumbers, bool set = true)
    {
        if (string.IsNullOrEmpty(achievementNumbers))
        {
            throw new ArgumentNullException(nameof(achievementNumbers));
        }

        if (!Handlers.TryGetValue(bot, out AchievementHandler? AchievementHandler))
        {
            ASFLogger.LogNullError(AchievementHandler);
            return null;
        }

        if (!uint.TryParse(appid, out uint appId))
        {
            return bot.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, nameof(appId)));
        }

        var achievements = new HashSet<uint>();

        var achievementStrings = achievementNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (!achievementNumbers.Equals("*"))
        {
            foreach (string achievement in achievementStrings)
            {
                if (!uint.TryParse(achievement, out uint achievementNumber) || (achievementNumber == 0))
                {
                    return bot.FormatBotResponse(string.Format(Strings.ErrorParsingObject, achievement));
                }

                achievements.Add(achievementNumber);
            }
            if (achievements.Count == 0)
            {
                return bot.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, "Achievements list"));
            }
        }
        return bot.FormatBotResponse(await Task.Run(() => AchievementHandler.SetAchievements(bot, appId, achievements, set)).ConfigureAwait(false));
    }

    /// <summary>
    /// 修改成就 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appid"></param>
    /// <param name="achievementNumbers"></param>
    /// <param name="set"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementSet(string botNames, string appid, string achievementNumbers, bool set = true)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementSet(bot, appid, achievementNumbers, set))).ConfigureAwait(false);

        List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }





    /// <summary>
    /// 获取成就数据列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appids"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementStatList(Bot bot, string appids)
    {
        if (!Handlers.TryGetValue(bot, out var AchievementHandler))
        {
            ASFLogger.LogNullError(AchievementHandler);
            return null;
        }

        var gameIDs = appids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (gameIDs.Length == 0)
        {
            return bot.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
        }

        if (AchievementHandler == null)
        {
            ASFLogger.LogNullError(AchievementHandler);
            return null;
        }

        var gamesToGetAchievements = new HashSet<uint>();

        foreach (string game in gameIDs)
        {
            if (!uint.TryParse(game, out uint gameID) || (gameID == 0))
            {
                return bot.FormatBotResponse(string.Format(Strings.ErrorParsingObject, nameof(gameID)));
            }

            gamesToGetAchievements.Add(gameID);
        }

        var results = await Utilities.InParallel(gamesToGetAchievements.Select(appID => Task.Run(() => AchievementHandler.GetAchievementStat(bot, appID)))).ConfigureAwait(false);

        List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? bot.FormatBotResponse(string.Join(Environment.NewLine, responses)) : null;


    }

    /// <summary>
    /// 获取成就数据列表 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appid"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementStatList(string botNames, string appid)
    {
        if (string.IsNullOrEmpty(botNames))
        {
            throw new ArgumentNullException(nameof(botNames));
        }

        var bots = Bot.GetBots(botNames);

        if (bots == null || bots.Count == 0)
        {
            return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementStatList(bot, appid))).ConfigureAwait(false);

        List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

}
