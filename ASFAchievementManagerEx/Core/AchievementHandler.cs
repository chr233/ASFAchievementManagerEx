using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ASFAchievementManagerEx.Core.Callbacks;
using SteamKit2;
using SteamKit2.Internal;

namespace ASFAchievementManagerEx.Core;

internal sealed class AchievementHandler : ClientMsgHandler
{
    /// <summary>
    /// 处理客户端消息
    /// </summary>
    /// <param name="packetMsg"></param>
    public override void HandleMsg(IPacketMsg packetMsg)
    {
        if (packetMsg == null)
        {
            ASFLogger.LogNullError(packetMsg);
            return;
        }

        switch (packetMsg.MsgType)
        {
            case EMsg.ClientGetUserStatsResponse:
                var getAchievementsResponse = new ClientMsgProtobuf<CMsgClientGetUserStatsResponse>(packetMsg);
                Client.PostCallback(new GetAchievementsCallback(packetMsg.TargetJobID, getAchievementsResponse.Body));
                break;

            case EMsg.ClientStoreUserStatsResponse:
                var setAchievementsResponse = new ClientMsgProtobuf<CMsgClientStoreUserStatsResponse>(packetMsg);
                Client.PostCallback(new SetAchievementsCallback(packetMsg.TargetJobID, setAchievementsResponse.Body));
                break;
        }
    }

    /// <summary>
    /// 获取成就数据
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="gameID"></param>
    /// <returns></returns>
    private async Task<(UserStatsData? data, uint crc_stats)> GetAchievementsResponse(Bot bot, ulong gameID)
    {
        if (!Client.IsConnected)
        {
            return (null, 0);
        }

        var request = new ClientMsgProtobuf<CMsgClientGetUserStats>(EMsg.ClientGetUserStats)
        {
            SourceJobID = Client.GetNextJobID(),
            Body = {
                game_id =  gameID,
                steam_id_for_user = bot.SteamID,
            },
        };

        Client.Send(request);

        var response = await new AsyncJob<GetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);
        if (response?.Response == null || !response.Success)
        {
            return (null, 0);
        }

        var data = ParseResponse(response.Response);
        return (data, response.Response.crc_stats);
    }

    private UserStatsData? ParseResponse(CMsgClientGetUserStatsResponse payload)
    {
        if (payload.schema == null)
        {
            ASFLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(payload.schema)));
            return null;
        }

        var keyValues = new KeyValue();
        using var ms = new MemoryStream(payload.schema);
        if (!keyValues.TryReadAsBinary(ms))
        {
            ASFLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(payload.schema)));
            return null;
        }

        var statsValueDict = new Dictionary<uint, uint>();
        foreach (var stat in payload.stats)
        {
            statsValueDict.TryAdd(stat.stat_id, stat.stat_value);
        }

        var depenciesDict = new Dictionary<string, AchievementData>();
        var achievementList = new List<AchievementData>();

        //first we enumerate all real achievements
        foreach (var stat in keyValues.FindEnumByName("stats"))
        {
            if (stat.ReadAsInt("type") == 4)
            {
                foreach (var bit in stat.FindEnumByName("bits"))
                {
                    if (int.TryParse(bit.Name, out var bitNum) && uint.TryParse(stat.Name, out var statId))
                    {
                        var statValue = payload?.stats?.Find(x => x.stat_id == statId)?.stat_value ?? 0;
                        var isUnlock = (statValue & ((uint)1 << bitNum)) != 0;

                        var permission = bit.ReadAsInt("permission", 0);

                        var process = bit.FindByName("process");
                        var dependancyName = bit.FindListByName("progress")?.FindListByName("value")?.FindByName("operand1")?.Value;

                        var dependancyValue = process?.ReadAsUInt("max_val") ?? 0;

                        var display = bit.FindByName("display")?.FindByName("name");

                        var name = display?.FindByName(Langs.Language)?.Value ?? display?.FindByName("english")?.Value;

                        var achievemet = new AchievementData
                        {
                            StatId = statId,
                            Bit = bitNum,
                            IsUnlock = isUnlock,
                            Permission = permission,
                            DependancyValue = dependancyValue,
                            DependancyName = dependancyName,
                            Dependancy = 0,
                            Name = name,
                            StatValue = statValue
                        };

                        achievementList.Add(achievemet);

                        if (!string.IsNullOrEmpty(dependancyName))
                        {
                            depenciesDict.TryAdd(dependancyName, achievemet);
                        }
                    }
                }
            }
        }

        var statsDict = new Dictionary<uint, StatsData>();

        //Now we update all dependancies
        foreach (var stat in keyValues.FindEnumByName("stats"))
        {
            if (stat.ReadAsInt("type", 0) == 1)
            {
                if (uint.TryParse(stat.Name, out var statId))
                {
                    var id = stat.ReadAsUInt("id", 0);

                    var name = stat.FindByName("name")?.Value;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    statsValueDict.TryGetValue(statId, out var statValue);

                    var permission = stat.ReadAsInt("permission", 0);
                    var def = stat.ReadAsInt("default", 0);
                    var maxChange = stat.ReadAsInt("maxchange");
                    var min = stat.ReadAsUInt("min");
                    var max = stat.ReadAsUInt("max");

                    var isIncrementonly = stat.FindByName("incrementonly")?.Value == "1";
                    var display = stat.FindByName("display")?.FindByName("name")?.Value;

                    if (depenciesDict.TryGetValue(name, out var parentStat))
                    {
                        parentStat.Dependancy = statId;
                    }

                    var stats = new StatsData
                    {
                        Id = id,
                        Name = display ?? "",
                        IsIncrementOnly = isIncrementonly,
                        Permission = permission,
                        Value = statValue,
                        Default = def,
                        MaxChange = maxChange,
                        Min = min,
                        Max = max,
                    };

                    if (!statsDict.TryAdd(id, stats))
                    {
                        ASFLogger.LogGenericWarning("id重复");
                    }
                }
            }
        }

        return new UserStatsData { Achievements = achievementList, Stats = statsDict };
    }

    /// <summary>
    /// 添加修改的成就
    /// </summary>
    /// <param name="achievements"></param>
    /// <param name="unlock"></param>
    private Dictionary<uint, CMsgClientStoreUserStats2.Stats> GetEffectAchievementDict(IEnumerable<AchievementData> achievements, bool unlock)
    {
        var effectedStatsDict = new Dictionary<uint, CMsgClientStoreUserStats2.Stats>();

        foreach (var achievement in achievements)
        {
            if (!effectedStatsDict.TryGetValue(achievement.StatId, out var currentstat))
            {
                currentstat = new CMsgClientStoreUserStats2.Stats()
                {
                    stat_id = achievement.StatId,
                    stat_value = achievement.StatValue
                };
                effectedStatsDict[achievement.StatId] = currentstat;
            }

            var statMask = (uint)1 << achievement.Bit;
            if (unlock)
            {
                currentstat.stat_value |= statMask;
            }
            else
            {
                currentstat.stat_value &= ~statMask;
            }

            if (!string.IsNullOrEmpty(achievement.DependancyName))
            {
                if (!effectedStatsDict.ContainsKey(achievement.Dependancy))
                {
                    var dependancystat = new CMsgClientStoreUserStats2.Stats()
                    {
                        stat_id = achievement.Dependancy,
                        stat_value = unlock ? achievement.DependancyValue : 0
                    };
                    effectedStatsDict[achievement.StatId] = dependancystat;
                }
            }
        }
        return effectedStatsDict;
    }

    /// <summary>
    /// 获取用户成就数据
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="gameID"></param>
    /// <returns></returns>
    internal async Task<UserStatsData?> GetUserStats(Bot bot, ulong gameID)
    {
        var (data, _) = await GetAchievementsResponse(bot, gameID);
        return data;
    }

    /// <summary>
    /// 获取用户成就数据
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="gameID"></param>
    /// <returns></returns>
    internal Task<(UserStatsData? userStats, uint crc_stats)> GetUserStatsWithCrcStats(Bot bot, ulong gameID)
    {
        return GetAchievementsResponse(bot, gameID);
    }

    /// <summary>
    /// 修改成就
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="appId"></param>
    /// <param name="crc_stats"></param>
    /// <param name="achievements"></param>
    /// <param name="unlock"></param>
    /// <returns></returns>
    internal async Task<bool?> SetAchievements(Bot bot, uint appId, uint crc_stats, IEnumerable<AchievementData> achievements, bool unlock)
    {
        var effectedStatsDict = GetEffectAchievementDict(achievements, unlock);

        if (effectedStatsDict.Any())
        {
            var request = new ClientMsgProtobuf<CMsgClientStoreUserStats2>(EMsg.ClientStoreUserStats2)
            {
                SourceJobID = Client.GetNextJobID(),
                Body = {
                    game_id =  appId,
                    settor_steam_id = bot.SteamID,
                    settee_steam_id = bot.SteamID,
                    explicit_reset = false,
                    crc_stats =crc_stats
                }
            };
            request.Body.stats.AddRange(effectedStatsDict.Values);
            Client.Send(request);

            var setResponse = await new AsyncJob<SetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);
            return setResponse?.Success;
        }
        else
        {
            return null;
        }
    }

}
