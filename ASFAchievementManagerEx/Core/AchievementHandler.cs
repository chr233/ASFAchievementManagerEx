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
                        var stat_value = payload?.stats?.Find(x => x.stat_id == statNum)?.stat_value;

                        var isSet = stat_value != null && (stat_value & (uint)1 << bitNum) != 0;

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
                            IsSet = isSet,
                            Restricted = permission != null,
                            DependancyValue = dependancyValue,
                            DependancyName = dependancyName,
                            Dependancy = 0,
                            Name = name,
                            StatValue = stat_value ?? 0
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

        var statusList = new List<StatusData>();

        //Now we update all dependancies
        foreach (var stat in keyValues.FindEnumByName("stats"))
        {
            if (stat.FindByName("type")?.Value == "1")
            {
                if (uint.TryParse(stat.Name, out var statNum))
                {
                    var restricted = stat.FindByName("permission") != null;
                    var name = stat.FindByName("name")?.Value;

                    var stat_value = payload?.stats?.Find(statElement => statElement.stat_id == statNum)?.stat_value;

                    var strPermission = stat.FindByName("permission")?.Value;
                    var incrementonly = stat.FindByName("incrementonly")?.Value;
                    var display = stat.FindByName("display")?.FindByName("name")?.Value;
                    var id = stat.FindByName("id")?.Value;

                    if (!int.TryParse(strPermission, out var permission))
                    {
                        permission = 0;
                    }
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

                        statusList.Add(new StatusData
                        {
                            Id = id ?? "",
                            DisplayName = display ?? "",
                            IsIncrementOnly = incrementonly == "1",
                            Permission = permission,
                            Value = stat_value ?? 0,
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

    //Endpoints

    internal async Task<(bool isConnected, UserStateData? data)> GetAchievements(Bot bot, ulong gameID)
    {
        if (!Client.IsConnected)
        {
            return (false, null);
        }

        var response = await GetAchievementsResponse(bot, gameID);

        if (response == null || response.Response == null || !response.Success)
        {
            return (true, null);
        }

        var data = ParseResponse(response.Response);
        return (true, data);
    }



    internal async Task<string> GetAchievementStat(Bot bot, ulong gameID)
    {
        if (!Client.IsConnected)
        {
            return Strings.BotNotConnected;
        }

        var response = await GetAchievementsResponse(bot, gameID);

        if (response == null || response.Response == null || !response.Success)
        {
            return "Can't retrieve achievements for " + gameID.ToString();
        }

        var responses = new List<string>();
        var data = ParseResponse(response.Response);
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

        var response = await GetAchievementsResponse(bot, appId);
        if (response?.Success != true)
        {
            bot.ArchiLogger.LogNullError(response);
            return "Can't retrieve achievements for " + appId.ToString(); ;
        }

        if (response.Response == null)
        {
            bot.ArchiLogger.LogNullError(response.Response);
            responses.Add(Strings.WarningFailed);
            return string.Join(Environment.NewLine, responses);
        }

        var data = ParseResponse(response.Response);
        var Stats = data?.Achievements;


        if (Stats == null)
        {
            responses.Add(Strings.WarningFailed);
            return string.Join(Environment.NewLine, responses);
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

                if (Stats[(int)achievement - 1].IsSet == set)
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
                    crc_stats = response.Response.crc_stats
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
        if (!Client.IsConnected)
        {
            return Strings.BotNotConnected;
        }

        var responses = new List<string>();

        var response = await GetAchievementsResponse(bot, appId);
        if (response == null)
        {
            bot.ArchiLogger.LogNullError(response);
            return "Can't retrieve achievements for " + appId.ToString(); ;
        }

        if (!response.Success)
        {
            return "Can't retrieve achievements for " + appId.ToString(); ;
        }

        if (response.Response == null)
        {
            bot.ArchiLogger.LogNullError(response.Response);
            responses.Add(Strings.WarningFailed);
            return "\u200B\n" + string.Join(Environment.NewLine, responses);
        }

        var data = ParseResponse(response.Response);
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

                if (Stats[(int)achievement - 1].IsSet == set)
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
                    crc_stats = response.Response.crc_stats
                }
        };
        request.Body.stats.AddRange(statsToSet);
        Client.Send(request);

        var setResponse = await new AsyncJob<SetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);

        responses.Add(setResponse?.Success ?? false ? Strings.Success : Strings.WarningFailed);
        return "\u200B\n" + string.Join(Environment.NewLine, responses);
    }


    private async Task<GetAchievementsCallback?> GetAchievementsResponse(Bot bot, ulong gameID)
    {
        if (!Client.IsConnected)
        {
            return null;
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

        return await new AsyncJob<GetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);
    }
}
