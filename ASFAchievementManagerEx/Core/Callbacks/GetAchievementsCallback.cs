using SteamKit2;
using SteamKit2.Internal;

namespace ASFAchievementManagerEx.Core.Callbacks;

internal sealed class GetAchievementsCallback : AbstrackMsgCallback<CMsgClientGetUserStatsResponse>
{
    internal GetAchievementsCallback(JobID jobID, CMsgClientGetUserStatsResponse msg)
        : base(jobID, msg, msg => (EResult)msg.eresult, "GetAchievements") { }
}
