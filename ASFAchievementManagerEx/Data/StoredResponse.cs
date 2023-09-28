using System;
using System.Collections.Generic;
using System.Text;
using SteamKit2.Internal;

namespace ASFAchievementManagerEx.Data;
internal sealed record StoredResponse
{
    public bool Success { get; set; }
    public CMsgClientGetUserStatsResponse? Response { get; set; }
}
