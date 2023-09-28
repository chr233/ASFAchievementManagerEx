using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFAchievementManagerEx.Core;
internal static class Command
{
    internal static ConcurrentDictionary<Bot, Handler> Handlers { get; private set; } = new();

    /// <summary>
    /// 显示成就列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appids"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementList(Bot bot, string appids)
    {
        var gameIDs = appids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (gameIDs.Length == 0)
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
        }
        if (Handlers.TryGetValue(bot, out Handler? AchievementHandler))
        {
            if (AchievementHandler == null)
            {
                bot.ArchiLogger.LogNullError(AchievementHandler);
                return null;
            }

            var gamesToGetAchievements = new HashSet<uint>();

            foreach (string game in gameIDs)
            {
                if (!uint.TryParse(game, out uint gameID) || (gameID == 0))
                {
                    return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorParsingObject, nameof(gameID)));
                }

                gamesToGetAchievements.Add(gameID);
            }

            var results = await Utilities.InParallel(gamesToGetAchievements.Select(appID => Task.Run<string>(() => AchievementHandler.GetAchievements(bot, appID)))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

            return responses.Count > 0 ? bot.Commands.FormatBotResponse(string.Join(Environment.NewLine, responses)) : null;
        }
        else
        {
            return bot.FormatBotResponse("未获取到成就信息");
        }
    }

    /// <summary>
    /// 获取成就列表 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appids"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseAchievementList(string botNames, string appids)
    {
        var bots = Bot.GetBots(botNames);

        if ((bots == null) || (bots.Count == 0))
        {
            return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementList(bot, appids))).ConfigureAwait(false);

        List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }


    internal static async Task<string?> ResponseAchievementSet(Bot bot, string appid, string achievementNumbers, bool set = true)
    {
        if (string.IsNullOrEmpty(achievementNumbers))
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorObjectIsNull, nameof(achievementNumbers)));
        }
        if (!uint.TryParse(appid, out uint appId))
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, nameof(appId)));
        }

        if (!Handlers.TryGetValue(bot, out Handler? AchievementHandler))
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(Handlers)));
        }

        if (AchievementHandler == null)
        {
            bot.ArchiLogger.LogNullError(AchievementHandler);
            return null;
        }

        var achievements = new HashSet<uint>();

        var achievementStrings = achievementNumbers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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

    internal static async Task<string?> ResponseAchievementSet(string botNames, string appid, string achievementNumbers, bool set = true)
    {

        var bots = Bot.GetBots(botNames);

        if ((bots == null) || (bots.Count == 0))
        {
            return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
        }

        var results = await Utilities.InParallel(bots.Select(bot => ResponseAchievementSet(bot, appid, achievementNumbers, set))).ConfigureAwait(false);

        List<string?> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }





    internal static async Task<string?> ResponseAchievementStatList(Bot bot, string appids)
    {
        var gameIDs = appids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (gameIDs.Length == 0)
        {
            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(gameIDs)));
        }
        if (Handlers.TryGetValue(bot, out Handler? AchievementHandler))
        {
            if (AchievementHandler == null)
            {
                bot.ArchiLogger.LogNullError(AchievementHandler);
                return null;
            }

            var gamesToGetAchievements = new HashSet<uint>();

            foreach (string game in gameIDs)
            {
                if (!uint.TryParse(game, out uint gameID) || (gameID == 0))
                {
                    return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorParsingObject, nameof(gameID)));
                }

                gamesToGetAchievements.Add(gameID);
            }

            var results = await Utilities.InParallel(gamesToGetAchievements.Select(appID => Task.Run(() => AchievementHandler.GetAchievementStat(bot, appID)))).ConfigureAwait(false);

            List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result)));

            return responses.Count > 0 ? bot.Commands.FormatBotResponse(string.Join(Environment.NewLine, responses)) : null;

        }
        else
        {

            return bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsEmpty, nameof(Handlers)));
        }
    }
}
