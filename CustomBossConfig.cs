using System.Collections.Generic;

namespace BossNotifier
{
    public class CustomBossConfig
    {
        public List<CustomBossGroup> customBossGroups { get; set; }
    }

    public class CustomBossGroup
    {
        public string groupName { get; set; }
        public string pluginGUID { get; set; }
        public bool isPlural { get; set; } = true;
        public List<CustomBossMember> members { get; set; }
    }

    public class CustomBossMember
    {
        public int id { get; set; }
        public string displayName { get; set; }
    }
}