using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFAchievementManagerEx.Data;
public sealed record PluginConfig
{
    public bool EULA { get; set; } = true;
    public bool Statistic { get; set; } = true;

    public List<string>? DisabledCmds { get; set; }
}
