using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ASFAchievementManagerEx.Core.Callbacks;
using ASFAchievementManagerEx.Localization;
using SteamKit2;
using SteamKit2.Internal;
using System.Text;

namespace ASFAchievementManagerEx.Core;

public sealed class AchievementHandler : ClientMsgHandler
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

    private async Task<(UserStateData? data, uint crc_stats)> GetAchievementsResponse(Bot bot, ulong gameID)
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

    private UserStateData? ParseResponse(CMsgClientGetUserStatsResponse payload)
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

        var statsDict = new Dictionary<uint, uint>();
        foreach (var stat in payload.stats)
        {
            statsDict.TryAdd(stat.stat_id, stat.stat_value);
        }

        var depenciesDict = new Dictionary<string, AchievementData>();
        var achievementList = new List<AchievementData>();

        //first we enumerate all real achievements
        foreach (var stat in keyValues.FindEnumByName("stats"))
        {
            if (stat.FindByName("type")?.Value == "4")
            {
                foreach (var bit in stat.FindEnumByName("bits"))
                {
                    if (int.TryParse(bit.Name, out var bitNum) && uint.TryParse(stat.Name, out var statNum))
                    {
                        var statValue = payload?.stats?.Find(x => x.stat_id == statNum)?.stat_value ?? 0;
                        var isUnlock = (statValue & (uint)1 << bitNum) != 0;

                        var permission = bit.FindByName("permission");

                        var process = bit.FindByName("process");
                        var dependancyName = bit.FindListByName("progress")?.FindListByName("value")?.FindByName("operand1")?.Value;

                        if (!uint.TryParse(process?.FindByName("max_val")?.Value, out var dependancyValue))
                        {
                            dependancyValue = 0;
                        }

                        var display = bit.FindByName("display")?.FindByName("name");

                        var name = display?.FindByName(Langs.Language)?.Value ?? display?.FindByName("english")?.Value;

                        var achievemet = new AchievementData
                        {
                            StatNum = statNum,
                            BitNum = bitNum,
                            IsUnlock = isUnlock,
                            Restricted = permission != null,
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

        var statusList = new List<StatsData>();

        var tmps = new List<KeyValue>();

        //Now we update all dependancies
        foreach (var stat in keyValues.FindEnumByName("stats"))
        {
            if (stat.FindByName("type")?.Value == "1")
            {
                if (uint.TryParse(stat.Name, out var statNum))
                {
                    var count = stat.Children.Count;
                    if (count > 5)
                    {
                        tmps.Add(stat);
                    }


                    var id = stat.ReadAsInt("id");

                    var name = stat.FindByName("name")?.Value;

                    statsDict.TryGetValue(statNum, out var statValue);

                    var permission = stat.ReadAsInt("permission", 0);
                    var def = stat.ReadAsInt("default", 0);
                    var maxChange = stat.ReadAsInt("maxchange", 0);
                    var min = stat.ReadAsInt("min", 0);

                    var restricted = permission != 0;

                    var isIncrementonly = stat.FindByName("incrementonly")?.Value == "1";
                    var display = stat.FindByName("display")?.FindByName("name")?.Value;


                    if (name != null)
                    {
                        if (depenciesDict.TryGetValue(name, out var parentStat))
                        {
                            parentStat.Dependancy = statNum;
                            if (restricted && !parentStat.Restricted)
                            {
                                parentStat.Restricted = true;
                            }
                        }

                        statusList.Add(new StatsData
                        {
                            Id = id,
                            Name = display ?? "",
                            IsIncrementOnly = isIncrementonly,
                            Permission = permission,
                            Value = statValue,
                            Default = def,
                            MaxChange = maxChange,
                            Min = min,
                        });
                    }
                    else
                    {

                    }
                }
            }
        }

        return new UserStateData { Achievements = achievementList, Stats = statusList };
    }

    private IEnumerable<CMsgClientStoreUserStats2.Stats> GetStatsToSet(List<CMsgClientStoreUserStats2.Stats> statsToSet, AchievementData statToSet, bool set = true)
    {
        if (statToSet == null)
        {
            yield break; //it should never happen
        }

        var currentstat = statsToSet.Find(stat => stat.stat_id == statToSet.StatNum);
        if (currentstat == null)
        {
            currentstat = new CMsgClientStoreUserStats2.Stats()
            {
                stat_id = statToSet.StatNum,
                stat_value = statToSet.StatValue
            };
            yield return currentstat;
        }

        var statMask = (uint)1 << statToSet.BitNum;
        if (set)
        {
            currentstat.stat_value |= statMask;
        }
        else
        {
            currentstat.stat_value &= ~statMask;
        }
        if (!string.IsNullOrEmpty(statToSet.DependancyName))
        {
            var dependancystat = statsToSet.Find(stat => stat.stat_id == statToSet.Dependancy);
            if (dependancystat == null)
            {
                dependancystat = new CMsgClientStoreUserStats2.Stats()
                {
                    stat_id = statToSet.Dependancy,
                    stat_value = set ? statToSet.DependancyValue : 0
                };
                yield return dependancystat;
            }
        }

    }


    /// <summary>
    /// 获取用户成就数据
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="gameID"></param>
    /// <returns></returns>
    internal async Task<UserStateData?> GetUserStates(Bot bot, ulong gameID)
    {
        var (data, _) = await GetAchievementsResponse(bot, gameID);
        return data;
    }


    internal async Task<string> GetAchievementStat(Bot bot, ulong gameID)
    {
        var responses = new List<string>();
        var (data, _) = await GetAchievementsResponse(bot, gameID);
        var Stats = data?.Stats;

        if (Stats == null)
        {
            bot.ArchiLogger.LogNullError(Stats);
        }
        else if (Stats.Count == 0)
        {
            bot.ArchiLogger.LogNullError(null, nameof(Stats));
        }
        else
        {
            foreach (var stat in Stats)
            {
                responses.Add(string.Format("{0,-5} {1}", Stats.IndexOf(stat) + 1, stat.ToString()));
            }
        }
        return responses.Count > 0 ? "\u200B\nAchievements for " + gameID.ToString() + ":\n" + string.Join(Environment.NewLine, responses) : "Can't retrieve achievements for " + gameID.ToString();
    }

    internal async Task<string> SetAchievementsStats(Bot bot, uint appId, HashSet<uint> achievements, bool set = true)
    {
        if (!Client.IsConnected)
        {
            return Strings.BotNotConnected;
        }

        var responses = new List<string>();

        var (data, crc_stats) = await GetAchievementsResponse(bot, appId);
        var Stats = data?.Achievements;


        if (Stats == null)
        {
            responses.Add(Strings.WarningFailed);
            return "Can't retrieve achievements for " + appId.ToString(); ;
        }

        var statsToSet = new List<CMsgClientStoreUserStats2.Stats>();

        if (achievements.Count == 0)
        {
            foreach (var stat in Stats.Where(s => !s.Restricted))
            {
                statsToSet.AddRange(GetStatsToSet(statsToSet, stat, set));
            }
        }
        else
        {
            foreach (var achievement in achievements)
            {
                if (Stats.Count < achievement)
                {
                    responses.Add("Achievement #" + achievement.ToString() + " is out of range");
                    continue;
                }

                if (Stats[(int)achievement - 1].IsUnlock == set)
                {
                    responses.Add("Achievement #" + achievement.ToString() + " is already " + (set ? "unlocked" : "locked"));
                    continue;
                }
                if (Stats[(int)achievement - 1].Restricted)
                {
                    responses.Add("Achievement #" + achievement.ToString() + " is protected and can't be switched");
                    continue;
                }

                statsToSet.AddRange(GetStatsToSet(statsToSet, Stats[(int)achievement - 1], set));
            }
        }

        if (statsToSet.Count == 0)
        {
            responses.Add(Strings.WarningFailed);
            return "\u200B\n" + string.Join(Environment.NewLine, responses);
        };
        if (responses.Count > 0)
        {
            responses.Add("Trying to switch remaining achievements..."); //if some errors occured
        }
        var request = new ClientMsgProtobuf<CMsgClientStoreUserStats2>(EMsg.ClientStoreUserStats2)
        {
            SourceJobID = Client.GetNextJobID(),
            Body = {
                    game_id =  appId,
                    settor_steam_id = bot.SteamID,
                    settee_steam_id = bot.SteamID,
                    explicit_reset = false,
                    crc_stats = crc_stats
                }
        };
        request.Body.stats.AddRange(statsToSet);
        Client.Send(request);

        var setResponse = await new AsyncJob<SetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);

        responses.Add(setResponse?.Success ?? false ? Strings.Success : Strings.WarningFailed);
        return "\u200B\n" + string.Join(Environment.NewLine, responses);
    }


    internal async Task<string> SetAchievements(Bot bot, uint appId, HashSet<uint> achievements, bool set = true)
    {
        var responses = new List<string>();

        var (data, crc_stats) = await GetAchievementsResponse(bot, appId);
        if (data == null)
        {
            bot.ArchiLogger.LogNullError(data);
            return "Can't retrieve achievements for " + appId.ToString(); ;
        }

        var Stats = data?.Achievements;

        if (Stats == null)
        {
            responses.Add(Strings.WarningFailed);
            return "\u200B\n" + string.Join(Environment.NewLine, responses);
        }

        var statsToSet = new List<CMsgClientStoreUserStats2.Stats>();

        if (achievements.Count == 0)
        {
            foreach (var stat in Stats.Where(s => !s.Restricted))
            {
                statsToSet.AddRange(GetStatsToSet(statsToSet, stat, set));
            }
        }
        else
        {
            foreach (var achievement in achievements)
            {
                if (Stats.Count < achievement)
                {
                    responses.Add("Achievement #" + achievement.ToString() + " is out of range");
                    continue;
                }

                if (Stats[(int)achievement - 1].IsUnlock == set)
                {
                    responses.Add("Achievement #" + achievement.ToString() + " is already " + (set ? "unlocked" : "locked"));
                    continue;
                }
                if (Stats[(int)achievement - 1].Restricted)
                {
                    responses.Add("Achievement #" + achievement.ToString() + " is protected and can't be switched");
                    continue;
                }

                statsToSet.AddRange(GetStatsToSet(statsToSet, Stats[(int)achievement - 1], set));
            }
        }

        if (statsToSet.Count == 0)
        {
            responses.Add(Strings.WarningFailed);
            return "\u200B\n" + string.Join(Environment.NewLine, responses);
        };
        if (responses.Count > 0)
        {
            responses.Add("Trying to switch remaining achievements..."); //if some errors occured
        }
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
        request.Body.stats.AddRange(statsToSet);
        Client.Send(request);

        var setResponse = await new AsyncJob<SetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);

        responses.Add(setResponse?.Success ?? false ? Strings.Success : Strings.WarningFailed);
        return "\u200B\n" + string.Join(Environment.NewLine, responses);
    }



}
