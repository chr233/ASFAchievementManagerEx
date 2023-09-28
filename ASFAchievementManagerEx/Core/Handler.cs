using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Localization;
using SteamKit2;
using SteamKit2.Internal;
using ASFAchievementManagerEx.Data;
using ASFAchievementManagerEx.Localization;
using System.Text;

namespace ASFAchievementManagerEx.Core;

public sealed class Handler : ClientMsgHandler
{
    public override void HandleMsg(IPacketMsg packetMsg)
    {
        if (packetMsg == null)
        {
            ASF.ArchiLogger.LogNullError(packetMsg);

            return;
        }

        switch (packetMsg.MsgType)
        {
            case EMsg.ClientGetUserStatsResponse:
                ClientMsgProtobuf<CMsgClientGetUserStatsResponse> getAchievementsResponse = new(packetMsg);
                Client.PostCallback(new GetAchievementsCallback(packetMsg.TargetJobID, getAchievementsResponse.Body));
                break;
            case EMsg.ClientStoreUserStatsResponse:
                ClientMsgProtobuf<CMsgClientStoreUserStatsResponse> setAchievementsResponse = new(packetMsg);
                Client.PostCallback(new SetAchievementsCallback(packetMsg.TargetJobID, setAchievementsResponse.Body));
                break;
        }

    }

    internal abstract class AchievementsCallBack<T> : CallbackMsg
    {
        internal readonly T Response;
        internal readonly bool Success;

        internal AchievementsCallBack(JobID jobID, T msg, Func<T, EResult> eresultGetter, string error)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            JobID = jobID ?? throw new ArgumentNullException(nameof(jobID));
            Success = eresultGetter(msg) == EResult.OK;
            Response = msg;

            if (!Success)
                ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorFailingRequest, error));
        }

    }

    internal sealed class GetAchievementsCallback : AchievementsCallBack<CMsgClientGetUserStatsResponse>
    {
        internal GetAchievementsCallback(JobID jobID, CMsgClientGetUserStatsResponse msg)
            : base(jobID, msg, msg => (EResult)msg.eresult, "GetAchievements") { }
    }

    internal sealed class SetAchievementsCallback : AchievementsCallBack<CMsgClientStoreUserStatsResponse>
    {
        internal SetAchievementsCallback(JobID jobID, CMsgClientStoreUserStatsResponse msg)
            : base(jobID, msg, msg => (EResult)msg.eresult, "SetAchievements") { }
    }

    //Utilities

    private List<StatData>? ParseResponse(CMsgClientGetUserStatsResponse Response)
    {
        var result = new List<StatData>();
        var KeyValues = new KeyValue();
        if (Response.schema != null)
        {
            using (var ms = new MemoryStream(Response.schema))
            {
                if (!KeyValues.TryReadAsBinary(ms))
                {
                    ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(Response.schema)));
                    return null;
                };
            }

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

            foreach (var child in KeyValues.Children.Find(Child => Child.Name == "stats")?.Children ?? new List<KeyValue>())
            {
                var key = child.Children.Find(Child => Child.Name == "type")?.Value;
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
            foreach (var stat in KeyValues.Children.Find(Child => Child.Name == "stats")?.Children ?? new List<KeyValue>())
            {
                if (stat.Children.Find(Child => Child.Name == "type")?.Value == "4")
                {
                    foreach (var Achievement in stat.Children.Find(Child => Child.Name == "bits")?.Children ?? new List<KeyValue>())
                    {
                        if (int.TryParse(Achievement.Name, out var bitNum))
                        {
                            if (uint.TryParse(stat.Name, out var statNum))
                            {
                                var stat_value = Response?.stats?.Find(statElement => statElement.stat_id == statNum)?.stat_value;
                                var isSet = stat_value != null && (stat_value & (uint)1 << bitNum) != 0;

                                var restricted = Achievement.Children.Find(Child => Child.Name == "permission") != null;

                                var dependancyName = Achievement.Children.Find(Child => Child.Name == "progress") == null ? "" : Achievement.Children.Find(Child => Child.Name == "progress")?.Children?.Find(Child => Child.Name == "value")?.Children?.Find(Child => Child.Name == "operand1")?.Value;

                                uint.TryParse(Achievement.Children.Find(Child => Child.Name == "progress") == null ? "0" : Achievement.Children.Find(Child => Child.Name == "progress")!.Children.Find(Child => Child.Name == "max_val")?.Value, out var dependancyValue);
                                var lang = CultureInfo.CurrentUICulture.EnglishName.ToLower();
                                if (lang.IndexOf('(') > 0)
                                {
                                    lang = lang.Substring(0, lang.IndexOf('(') - 1);
                                }
                                if (Achievement.Children.Find(Child => Child.Name == "display")?.Children?.Find(Child => Child.Name == "name")?.Children?.Find(Child => Child.Name == lang) == null)
                                {
                                    lang = "english";//fallback to english
                                }

                                var name = Achievement.Children.Find(Child => Child.Name == "display")?.Children?.Find(Child => Child.Name == "name")?.Children?.Find(Child => Child.Name == lang)?.Value;
                                result.Add(new StatData
                                {
                                    StatNum = statNum,
                                    BitNum = bitNum,
                                    IsSet = isSet,
                                    Restricted = restricted,
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
            }
            //Now we update all dependancies
            foreach (var stat in KeyValues.Children.Find(Child => Child.Name == "stats")?.Children ?? new List<KeyValue>())
            {
                if (stat.Children.Find(Child => Child.Name == "type")?.Value == "1")
                {
                    if (uint.TryParse(stat.Name, out var statNum))
                    {
                        var restricted = stat.Children.Find(Child => Child.Name == "permission") != null;
                        var name = stat.Children.Find(Child => Child.Name == "name")?.Value;
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
                    ASF.ArchiLogger.LogGenericError(string.Format(Strings.ErrorIsInvalid, nameof(Response.schema)));
                    return null;
                };
            }

            //first we enumerate all real achievements

            //Now we update all dependancies
            foreach (var stat in KeyValues.Children.Find(Child => Child.Name == "stats")?.Children ?? new List<KeyValue>())
            {
                if (stat.Children.Find(Child => Child.Name == "type")?.Value == "1")
                {
                    if (uint.TryParse(stat.Name, out var statNum))
                    {
                        var stat_value = Response?.stats?.Find(statElement => statElement.stat_id == statNum)?.stat_value;

                        var strPermission = stat.Children.Find(Child => Child.Name == "permission")?.Value;
                        var name = stat.Children.Find(Child => Child.Name == "name")?.Value;
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
                }
        };

        Client.Send(request);

        return await new AsyncJob<GetAchievementsCallback>(Client, request.SourceJobID).ToLongRunningTask().ConfigureAwait(false);
    }
}
