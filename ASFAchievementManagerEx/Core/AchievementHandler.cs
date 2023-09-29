using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using ASFAchievementManagerEx.Core.Callbacks;
using ASFAchievementManagerEx.Localization;
using SteamKit2;
using SteamKit2.Internal;
using System.Globalization;
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

    //Utilities

    private List<StatData>? ParseResponse(CMsgClientGetUserStatsResponse Response)
    {
        var result = new List<StatData>();
        var keyValues = new KeyValue();
        if (Response.schema != null)
        {
            using var ms = new MemoryStream(Response.schema);
            if (!keyValues.TryReadAsBinary(ms))
            {
                ASFLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(Response.schema)));
                return null;
            };

            var dic = new Dictionary<string, int>
            {
                {"1",0},
                {"2",0},
                {"3",0},
                {"4",0},
                {"5",0},
                {"6",0},
                {"7",0},
                {"E",0}
            };

            foreach (var child in keyValues.FindEnumByName("stats"))
            {
                var key = child.FindByName("type")?.Value;
                switch (key)
                {
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                        dic[key]++;
                        break;
                    default:
                        dic["E"]++;
                        break;
                }
            }

            //first we enumerate all real achievements
            foreach (var stat in keyValues.FindEnumByName("stats"))
            {
                if (stat.FindByName("type")?.Value == "4")
                {
                    foreach (var achievement in stat.FindEnumByName("bits"))
                    {
                        if (int.TryParse(achievement.Name, out var bitNum) && uint.TryParse(stat.Name, out var statNum))
                        {
                            var stat_value = Response?.stats?.Find(x => x.stat_id == statNum)?.stat_value;

                            var isSet = stat_value != null && (stat_value & (uint)1 << bitNum) != 0;

                            var permission = achievement.FindByName("permission");

                            var process = achievement.FindByName("process");
                            var dependancyName = achievement.FindListByName("progress")?.FindListByName("value")?.FindByName("operand1")?.Value;

                            uint.TryParse(process == null ? "0" : achievement.FindByName("progress")!.FindByName("max_val")?.Value, out var dependancyValue);
                            var lang = CultureInfo.CurrentUICulture.EnglishName.ToLower();
                            if (lang.IndexOf('(') > 0)
                            {
                                lang = lang.Substring(0, lang.IndexOf('(') - 1);
                            }
                            if (achievement.FindByName("display")?.Children?.Find(static x => x.Name == "name")?.Children?.Find(Child => Child.Name == lang) == null)
                            {
                                lang = "english";//fallback to english
                            }

                            var name = achievement.FindByName("display")?.Children?.Find(static x => x.Name == "name")?.Children?.Find(Child => Child.Name == lang)?.Value;
                            result.Add(new StatData
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
                            });

                        }
                    }
                }
            }
            //Now we update all dependancies
            foreach (var stat in keyValues.FindEnumByName("stats"))
            {
                if (stat.FindByName("type")?.Value == "1")
                {
                    if (uint.TryParse(stat.Name, out var statNum))
                    {
                        var restricted = stat.FindByName("permission") != null;
                        var name = stat.FindByName("name")?.Value;
                        if (name != null)
                        {
                            var ParentStat = result.Find(item => item.DependancyName == name);
                            if (ParentStat != null)
                            {
                                ParentStat.Dependancy = statNum;
                                if (restricted && !ParentStat.Restricted)
                                {
                                    ParentStat.Restricted = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        return result;
    }


    private List<IntStatInfo>? ParseResponseEx(CMsgClientGetUserStatsResponse Response)
    {
        var result = new List<IntStatInfo>();
        var KeyValues = new KeyValue();
        if (Response.schema != null)
        {
            using (var ms = new MemoryStream(Response.schema))
            {
                if (!KeyValues.TryReadAsBinary(ms))
                {
                    ASFLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(Response.schema)));
                    return null;
                };
            }

            //Now we update all dependancies
            foreach (var stat in KeyValues.FindEnumByName("stats"))
            {
                if (stat.FindByName("type")?.Value == "1")
                {
                    if (uint.TryParse(stat.Name, out var statNum))
                    {
                        var stat_value = Response?.stats?.Find(statElement => statElement.stat_id == statNum)?.stat_value;

                        var strPermission = stat.FindByName("permission")?.Value;
                        var name = stat.FindByName("name")?.Value;
                        var incrementonly = stat.Children.Find(x => x.Name == "incrementonly")?.Value;
                        var display = stat.Children.Find(x => x.Name == "display")
                            ?.Children.Find(x => x.Name == "name")?.Value;
                        var id = stat.Children.Find(x => x.Name == "id")?.Value;

                        if (!int.TryParse(strPermission, out var permission))
                        {
                            permission = -999;
                        }


                        if (name != null)
                        {
                            result.Add(new IntStatInfo
                            {
                                Id = id ?? "",
                                DisplayName = display ?? "",
                                IsIncrementOnly = incrementonly == "1",
                                Permission = permission,
                                Value = stat_value ?? 0,
                            });
                        }
                    }
                }
            }
        }

        return result;
    }




    private IEnumerable<CMsgClientStoreUserStats2.Stats> GetStatsToSet(List<CMsgClientStoreUserStats2.Stats> statsToSet, StatData statToSet, bool set = true)
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

    internal async Task<string> GetAchievements(Bot bot, ulong gameID)
    {
        if (!Client.IsConnected)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        var response = await GetAchievementsResponse(bot, gameID);

        if (response == null || response.Response == null || !response.Success)
        {
            return "Can't retrieve achievements for " + gameID.ToString();
        }

        var sb = new StringBuilder();
        sb.AppendLine(bot.FormatBotResponse(Langs.MultipleLineResult));
        var Stats = ParseResponse(response.Response);

        if (Stats?.Count > 0)
        {
            foreach (var stat in Stats)
            {
                sb.AppendLine(string.Format("{0,-3} {1} {2}{3}", Stats.IndexOf(stat) + 1, stat.IsSet ? Static.Yes : Static.No, stat.Name, stat.Restricted ? "\u26A0\uFE0F " : ""));
            }
            return sb.ToString();
        }
        else
        {
            return bot.FormatBotResponse("Can't retrieve achievements for " + gameID.ToString());
        }
    }

    internal async Task<string> GetAchievementStat(Bot bot, ulong gameID)
    {
        if (!Client.IsConnected)
        {
            return bot.FormatBotResponse(Strings.BotNotConnected);
        }

        var response = await GetAchievementsResponse(bot, gameID);

        if (response == null || response.Response == null || !response.Success)
        {
            return "Can't retrieve achievements for " + gameID.ToString();
        }

        var responses = new List<string>();
        var Stats = ParseResponseEx(response.Response);
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
            return bot.FormatBotResponse(Strings.BotNotConnected);
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
            return "" + string.Join(Environment.NewLine, responses);
        }

        var Stats = ParseResponse(response.Response);
        if (Stats == null)
        {
            responses.Add(Strings.WarningFailed);
            return "" + string.Join(Environment.NewLine, responses);
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
            return bot.FormatBotResponse(Strings.BotNotConnected);
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

        var Stats = ParseResponse(response.Response);
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
