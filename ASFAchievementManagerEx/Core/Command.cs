using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Interaction;
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
    /// <param name="appIds"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetAchievementList(Bot bot, string appIds)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
        {
            return Langs.InternalError;
        }

        var sb = new StringBuilder();

        var entries = appIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            if (!uint.TryParse(entry, out uint gameId) || (gameId == 0))
            {
                sb.AppendLine(bot.FormatBotResponse(Langs.ArgsInvalid, nameof(gameId), gameId));
            }
            else
            {
                try
                {
                    var userStats = await handler.GetUserStats(bot, gameId).ConfigureAwait(false);
                    var achievements = userStats?.Achievements;

                    if (achievements?.Count > 0)
                    {
                        sb.AppendLine(bot.FormatBotResponse(Langs.AchievementList, entry));

                        var id = 1;
                        foreach (var achievement in achievements)
                        {
                            sb.AppendLineFormat(
                                Langs.AchievementItem,
                                id++,
                                achievement.IsUnlock ? Langs.EmojiYes : Langs.EmojiNo,
                                achievement.Name,
                                achievement.IsProtected ? Langs.EmojiLock : ""
                            );
                        }
                    }
                    else
                    {
                        sb.AppendLine(bot.FormatBotResponse(bot.IsConnectedAndLoggedOn ? string.Format(Langs.GetAchievementDataFailure, entry) : Strings.BotNotConnected));
                    }
                }
                catch (Exception ex)
                {
                    ASFLogger.LogGenericException(ex);
                    sb.AppendLine(bot.FormatBotResponse(Langs.GetAchievementDataError, entry));
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
    /// <param name="appIds"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetStatsList(Bot bot, string appIds)
    {
        if (!Handlers.TryGetValue(bot, out var handler))
        {
            return Langs.InternalError;
        }

        var sb = new StringBuilder();

        var entries = appIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            if (!uint.TryParse(entry, out uint gameId) || (gameId == 0))
            {
                sb.AppendLine(bot.FormatBotResponse(Langs.ArgsInvalid, nameof(gameId), gameId));
            }
            else
            {
                try
                {
                    var userStats = await handler.GetUserStats(bot, gameId).ConfigureAwait(false);
                    var statsDict = userStats?.Stats;

                    if (statsDict?.Count > 0)
                    {
                        sb.AppendLine(bot.FormatBotResponse(Langs.StatsList, entry));
                        sb.AppendLine(Langs.StatsTitle);

                        foreach (var (id, stats) in statsDict)
                        {
                            sb.AppendLineFormat(
                                Langs.StatsItem,
                                id,
                                stats.StrValue,
                                stats.Name,
                                stats.IsProtected ? Langs.EmojiLock : "",
                                stats.IsIncrementOnly ? Langs.EmojiIncrementOnly : "",
                                stats.MaxChange != null ? string.Format(Langs.MaxChange, Langs.EmojiWarning, stats.MaxChange) : ""
                            );
                        }
                    }
                    else
                    {
                        sb.AppendLine(bot.FormatBotResponse(bot.IsConnectedAndLoggedOn ? string.Format(Langs.GetAchievementDataFailure, entry) : Strings.BotNotConnected));
                    }
                }
                catch (Exception e)
                {
                    sb.AppendLine(bot.FormatBotResponse(Langs.GetAchievementDataError, entry));
                    sb.AppendLine(e.ToString());
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
    /// <param name="appId"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseGetStatsList(string botNames, string appId)
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

        var results = await Utilities.InParallel(bots.Select(bot => ResponseGetStatsList(bot, appId))).ConfigureAwait(false);

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
            return bot.FormatBotResponse(Langs.ArgsInvalid, nameof(appId), appId);
        }

        try
        {
            var (userStats, crc_status) = await handler.GetUserStatsWithCrcStats(bot, gameId).ConfigureAwait(false);
            var achievementList = userStats?.Achievements;

            if (achievementList == null)
            {
                return bot.FormatBotResponse(Langs.GetAchievementDataFailure, appId);
            }

            var effectedAchievements = new HashSet<AchievementData>();

            var warnings = new StringBuilder();

            if (query != "*")
            {
                var entries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in entries)
                {
                    if (int.TryParse(entry, out var index))
                    {
                        if (achievementList.Count < index || index <= 0)
                        {
                            warnings.AppendLineFormat(Langs.AchievementNotFounf, index);
                        }
                        else
                        {
                            var achievement = achievementList[index - 1];

                            if (achievement.IsUnlock == unlock)
                            {
                                warnings.AppendLineFormat(Langs.AchievementChangeUnnecessary, index, achievement.Name);
                            }
                            else if (achievement.IsProtected)
                            {
                                warnings.AppendLineFormat(Langs.AchievementIsProtected, index, achievement.Name);
                            }
                            else
                            {
                                effectedAchievements.Add(achievement);
                            }
                        }
                    }
                    else
                    {
                        warnings.AppendLineFormat(Langs.AchievementIdInvalid, entry);
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
                sb.AppendLine(Langs.WarningInfo);
                sb.AppendLine(warnings.ToString());
                sb.AppendLine(Langs.ExecuteResult);
            }

            if (effectedAchievements.Any())
            {
                var result = await handler.ModifyAchievements(bot, gameId, crc_status, effectedAchievements, unlock).ConfigureAwait(false);
                sb.AppendLineFormat(Langs.SetAchievementResult, result == true ? Langs.Success : Langs.Failure, effectedAchievements.Count);
            }
            else
            {
                sb.AppendLine(Langs.NoAchievementEffected);
            }

            return bot.FormatBotResponse(sb.ToString());
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            return bot.FormatBotResponse(Langs.GetAchievementDataError, appId);
        }
    }

    /// <summary>
    /// 修改成就 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appId"></param>
    /// <param name="achievementNumbers"></param>
    /// <param name="unlock"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSetAchievement(string botNames, string appId, string achievementNumbers, bool unlock = true)
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

        var results = await Utilities.InParallel(bots.Select(bot => ResponseSetAchievement(bot, appId, achievementNumbers, unlock))).ConfigureAwait(false);

        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }


    /// <summary>
    /// 修改统计数据
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appId"></param>
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
            return bot.FormatBotResponse(Langs.ArgsInvalid, nameof(appId), appId);
        }

        try
        {
            var (userStats, crc_status) = await handler.GetUserStatsWithCrcStats(bot, gameId).ConfigureAwait(false);
            var statsDict = userStats?.Stats;

            if (statsDict == null)
            {
                return bot.FormatBotResponse(Langs.GetAchievementDataFailure, appId);
            }

            var effectedAchievements = new HashSet<StatsData>();

            var warnings = new StringBuilder();


            var entries = query.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var args = entry.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length < 2)
                {
                    warnings.AppendLineFormat(Langs.StatsArgumentInvalid, entry);
                    continue;
                }

                if (uint.TryParse(args[0], out var index))
                {
                    if (statsDict.TryGetValue(index, out var stat))
                    {
                        if (stat.IsProtected)
                        {
                            warnings.AppendLineFormat(Langs.StatsIsProtected, index, stat.Name, stat.StrValue);
                        }

                        uint? targetValue = null;
                        switch (args[1].ToLowerInvariant())
                        {
                            case "d":
                            case "default":
                                if (stat.Default == null)
                                {
                                    warnings.AppendLineFormat(Langs.StatsCantSetToDefault, stat.Id, stat.Name, stat.StrValue);
                                }
                                targetValue = stat.Default;
                                break;
                            case "i":
                            case "min":
                                if (stat.Min == null)
                                {
                                    warnings.AppendLineFormat(Langs.StatsCantSetToMin, stat.Id, stat.Name, stat.StrValue);
                                }
                                targetValue = stat.Min;
                                break;
                            case "a":
                            case "max":
                                if (stat.Max == null)
                                {
                                    warnings.AppendLineFormat(Langs.StatsCantSetToMax, stat.Id, stat.Name, stat.StrValue);
                                }
                                targetValue = stat.Max;
                                break;
                            default:
                                if (uint.TryParse(args[1], out var value))
                                {
                                    targetValue = value;
                                }
                                else
                                {
                                    warnings.AppendLineFormat(Langs.StatsTargetValueInvalid, stat.Id, stat.Name, stat.StrValue, args[1]);
                                }
                                break;
                        }

                        if (targetValue != null)
                        {
                            if (stat.Value == targetValue)
                            {
                                warnings.AppendLineFormat(Langs.StatsChangeUnnecessary, index, stat.Name, stat.StrValue);
                            }
                            else if (stat.IsIncrementOnly && stat.Value > targetValue)
                            {
                                warnings.AppendLineFormat(Langs.StatsIncrementOnlyLimited, index, stat.Name, stat.StrValue, targetValue);
                            }
                            else if (stat.MaxChange != null && Math.Abs((decimal)stat.Value - targetValue.Value) > stat.MaxChange)
                            {
                                warnings.AppendLineFormat(Langs.StatsMaxChangeLimited, index, stat.Name, stat.StrValue, targetValue, stat.MaxChange);
                                targetValue = stat.Value > targetValue ? stat.Value + stat.MaxChange : stat.Value - stat.MaxChange;
                            }

                            stat.Value = targetValue.Value;
                            effectedAchievements.Add(stat);
                        }
                    }
                    else
                    {
                        warnings.AppendLineFormat(Langs.StatsNotFound, index);
                    }
                }
                else
                {
                    warnings.AppendLineFormat(Langs.StatsArgumentInvalid, entry);
                }
            }

            var sb = new StringBuilder();
            if (warnings.Length > 0)
            {
                sb.AppendLine(Langs.MultipleLineResult);
                sb.AppendLine(Langs.WarningInfo);
                sb.AppendLine(warnings.ToString());
                sb.AppendLine(Langs.ExecuteResult);
            }

            if (effectedAchievements.Any())
            {
                var result = await handler.ModifyStats(bot, gameId, crc_status, effectedAchievements).ConfigureAwait(false);
                sb.AppendLineFormat(Langs.SetStatsResult, result == true ? Langs.Success : Langs.Failure, effectedAchievements.Count);
            }
            else
            {
                sb.AppendLine(Langs.NoStatsEffected);
            }

            return bot.FormatBotResponse(sb.ToString());
        }
        catch (Exception ex)
        {
            ASFLogger.LogGenericException(ex);
            return bot.FormatBotResponse(Langs.GetAchievementDataError, appId);
        }
    }

    /// <summary>
    /// 修改统计数据 (多个Bot)
    /// </summary>
    /// <param name="botNames"></param>
    /// <param name="appId"></param>
    /// <param name="query"></param>
    /// <returns></returns>
    internal static async Task<string?> ResponseSetStats(string botNames, string appId, string query)
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

        var results = await Utilities.InParallel(bots.Select(bot => ResponseSetStats(bot, appId, query))).ConfigureAwait(false);

        var responses = new List<string?>(results.Where(result => !string.IsNullOrEmpty(result)));

        return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
    }

}
