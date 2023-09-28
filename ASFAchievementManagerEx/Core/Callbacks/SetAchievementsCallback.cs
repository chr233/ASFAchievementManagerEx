using SteamKit2;
using SteamKit2.Internal;

namespace ASFAchievementManagerEx.Core.Callbacks;

internal sealed class SetAchievementsCallback : AbstrackMsgCallback<CMsgClientStoreUserStatsResponse>
{
    internal SetAchievementsCallback(JobID jobID, CMsgClientStoreUserStatsResponse msg)
        : base(jobID, msg, msg => (EResult)msg.eresult, "SetAchievements") { }
}
