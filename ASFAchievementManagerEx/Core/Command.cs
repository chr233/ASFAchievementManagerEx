using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
using ASFAchievementManagerEx.Localization;
using SteamKit2.Internal;
using System.Collections.Concurrent;
using System.Text;
using static SteamKit2.Internal.CMsgClientRequestedClientStats;

namespace ASFAchievementManagerEx.Core;
internal static class Command
{
    internal static ConcurrentDictionary<Bot, AchievementHandler> Handlers { get; private set; } = new();

    /// <summary>
    /// 显示成就列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appIds"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetAchievementList(Bot bot, string appIds)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
        {
            return Langs.InternalError;
        }

        var sb = new StringBuilder();

        var entries = appIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            if (!uint.TryParse(entry, out uint gameId) || (gameId == 0))
            {
                sb.AppendLine(bot.FormatBotResponse(string.Format(Langs.ArgsInvalid, nameof(gameId), gameId)));
            }
            else
            {
                var userStats = await handler.GetUserStats(bot, gameId).ConfigureAwait(false);
                var achievements = userStats?.Achievements;

                if (achievements?.Count > 0)
                {
                    sb.AppendLine(bot.FormatBotResponse(string.Format("App/{0} 的成就列表:", entry)));

                    var id = 1;
                    foreach (var achievement in achievements)
                    {
                        sb.AppendLine(string.Format(
                            "- {0,-3} {1} {2}{3}",
                            id++,
                            achievement.IsUnlock ? Static.Yes : Static.No,
                            achievement.Name,
                            achievement.IsProtected ? Static.Lock : ""
                        ));
                    }
                }
                else
                {
                    sb.AppendLine(bot.FormatBotResponse(bot.IsConnectedAndLoggedOn ? string.Format(Langs.GetAchievementDataFailure, entry) : Strings.BotNotConnected));
                }
            }

            sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    /// 获取成就列表 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appIds"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetAchievementList(string botNames, string appIds)
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

        var results = await Utilities.InParallel(bots.Select(bot => ResponseGetAchievementList(bot, appIds))).ConfigureAwait(false);
        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 获取成就数据列表
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appids"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetStatsList(Bot bot, string appids)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
        {
            return Langs.InternalError;
        }

        var sb = new StringBuilder();

        var entries = appids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            if (!uint.TryParse(entry, out uint gameId) || (gameId == 0))
            {
                sb.AppendLine(bot.FormatBotResponse(string.Format(Langs.ArgsInvalid, nameof(gameId), gameId)));
            }
            else
            {
                var userStats = await handler.GetUserStats(bot, gameId).ConfigureAwait(false);
                var statsDict = userStats?.Stats;

                if (statsDict?.Count > 0)
                {
                    sb.AppendLine(bot.FormatBotResponse(string.Format("App/{0} 的统计数据列表:", entry)));
                    sb.AppendLine(Langs.StatsTitle);

                    foreach (var (id, stats) in statsDict)
                    {
                        sb.AppendLine(string.Format(
                            "- {0,-3} [{1}] {2}{3}{4}{5}",
                            id,
                            stats.StrValue,
                            stats.Name,
                            stats.IsProtected ? Static.Lock : "",
                            stats.IsIncrementOnly ? Static.IncrementOnly : "",
                            stats.MaxChange != null ? string.Format(Langs.MaxChange, Static.Warning, stats.MaxChange) : ""
                        ));
                    }
                }
                else
                {
                    sb.AppendLine(bot.FormatBotResponse(bot.IsConnectedAndLoggedOn ? string.Format(Langs.GetAchievementDataFailure, entry) : Strings.BotNotConnected));
                }
            }

            sb.AppendLine();
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    /// 获取成就数据列表 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appid"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetStatsList(string botNames, string appid)
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

        var results = await Utilities.InParallel(bots.Select(bot => ResponseGetStatsList(bot, appid))).ConfigureAwait(false);

        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

    /// <summary>
    /// 修改成就
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appId"></param>
    /// <param name="query"></param>
    /// <param name="unlock"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSetAchievement(Bot bot, string appId, string query, bool unlock = true)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (!Handlers.TryGetValue(bot, out AchievementHandler? handler))
        {
            return Langs.InternalError;
        }

        if (!uint.TryParse(appId, out var gameId))
        {
            return bot.FormatBotResponse(string.Format(Langs.ArgsInvalid, nameof(appId), appId));
        }

        var (userStats, crc_status) = await handler.GetUserStatsWithCrcStats(bot, gameId).ConfigureAwait(false);
        var achievementList = userStats?.Achievements;

        if (achievementList == null)
        {
            return bot.FormatBotResponse(string.Format(Langs.GetAchievementDataFailure, appId));
        }

        var effectedAchievements = new HashSet<AchievementData>();

        var warnings = new StringBuilder();

        if (query != "*")
        {
            var entries = query.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                if (int.TryParse(entry, out var index))
                {
                    if (achievementList.Count < index || index <= 0)
                    {
                        warnings.AppendLine(string.Format("{0}: 无此ID的成就", index));
                    }
                    else
                    {
                        var achievement = achievementList[index - 1];

                        if (achievement.IsUnlock == unlock)
                        {
                            warnings.AppendLine(string.Format("{0}: 无需修改该项成就", index));
                        }
                        else if (achievement.IsProtected)
                        {
                            warnings.AppendLine(string.Format("{0}: 无法修改被保护的成就", index));
                        }
                        else
                        {
                            effectedAchievements.Add(achievement);
                        }
                    }
                }
                else
                {
                    warnings.AppendLine(string.Format("{0}: 无效参数, 需要为整数", entry));
                }
            }
        }
        else
        {
            foreach (var achievement in achievementList)
            {
                if (!achievement.IsProtected && achievement.IsUnlock != unlock)
                {
                    effectedAchievements.Add(achievement);
                }
            }
        }

        var sb = new StringBuilder();
        if (warnings.Length > 0)
        {
            sb.AppendLine(Langs.MultipleLineResult);
            sb.AppendLine("警告信息:");
            sb.AppendLine(warnings.ToString());
            sb.AppendLine("执行结果:");
        }

        if (effectedAchievements.Any())
        {
            var result = await handler.SetAchievements(bot, gameId, crc_status, effectedAchievements, unlock).ConfigureAwait(false);
            sb.AppendLine(string.Format("设置成就{0}, 受影响成就 {1} 个", result == true ? Langs.Success : Langs.Failure, effectedAchievements.Count));
        }
        else
        {
            sb.AppendLine("无待设置的成就");
        }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 修改成就 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appid"></param>
    /// <param name="achievementNumbers"></param>
    /// <param name="unlock"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSetAchievement(string botNames, string appid, string achievementNumbers, bool unlock = true)
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

        var results = await Utilities.InParallel(bots.Select(bot => ResponseSetAchievement(bot, appid, achievementNumbers, unlock))).ConfigureAwait(false);

        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }


    /// <summary>
    /// 修改统计数据
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appid"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSetStats(Bot bot, string appId, string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (!Handlers.TryGetValue(bot, out AchievementHandler? handler))
        {
            return Langs.InternalError;
        }

        if (!uint.TryParse(appId, out var gameId))
        {
            return bot.FormatBotResponse(string.Format(Langs.ArgsInvalid, nameof(appId), appId));
        }

        var (userStats, crc_status) = await handler.GetUserStatsWithCrcStats(bot, gameId).ConfigureAwait(false);
        var statsDict = userStats?.Stats;

        if (statsDict == null)
        {
            return bot.FormatBotResponse(string.Format(Langs.GetAchievementDataFailure, appId));
        }

        var effectedAchievements = new Dictionary<uint,uint>();

        var warnings = new StringBuilder();

        if (query != "*")
        {
            var entries = query.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                if (int.TryParse(entry, out var index))
                {
                    if (achievementList.Count < index || index <= 0)
                    {
                        warnings.AppendLine(string.Format("{0}: 无此ID的成就", index));
                    }
                    else
                    {
                        var achievement = achievementList[index - 1];

                        if (achievement.IsUnlock == unlock)
                        {
                            warnings.AppendLine(string.Format("{0}: 无需修改该项成就", index));
                        }
                        else if (achievement.IsProtected)
                        {
                            warnings.AppendLine(string.Format("{0}: 无法修改被保护的成就", index));
                        }
                        else
                        {
                            effectedAchievements.Add(achievement);
                        }
                    }
                }
                else
                {
                    warnings.AppendLine(string.Format("{0}: 无效参数, 需要为整数", entry));
                }
            }
        }
        else
        {
            foreach (var achievement in achievementList)
            {
                if (!achievement.IsProtected && achievement.IsUnlock != unlock)
                {
                    effectedAchievements.Add(achievement);
                }
            }
        }

        var sb = new StringBuilder();
        if (warnings.Length > 0)
        {
            sb.AppendLine(Langs.MultipleLineResult);
            sb.AppendLine("警告信息:");
            sb.AppendLine(warnings.ToString());
            sb.AppendLine("执行结果:");
        }

        if (effectedAchievements.Any())
        {
            var result = await handler.SetAchievements(bot, gameId, crc_status, effectedAchievements, unlock).ConfigureAwait(false);
            sb.AppendLine(string.Format("设置成就{0}, 受影响成就 {1} 个", result == true ? Langs.Success : Langs.Failure, effectedAchievements.Count));
        }
        else
        {
            sb.AppendLine("无待设置的成就");
        }

        return bot.FormatBotResponse(sb.ToString());
    }

    /// <summary>
    /// 修改统计数据 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appid"></param>
    /// <param name="kvSet"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSetStats(string botNames, string appid, string kvSet)
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

        var results = await Utilities.InParallel(bots.Select(bot => ResponseSetStats(bot, appid, kvSet))).ConfigureAwait(false);

        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

}
