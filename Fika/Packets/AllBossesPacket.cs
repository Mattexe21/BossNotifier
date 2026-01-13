using Fika.Core.Networking.LiteNetLib.Utils;
using System.Collections.Generic;

namespace BossNotifier.Fika.Packets
{
    public struct AllBossesPacket : INetSerializable
    {
        public Dictionary<string, string> BossesInRaid;

        public AllBossesPacket(Dictionary<string, string> bossesInRaid)
        {
            BossesInRaid = bossesInRaid ?? new Dictionary<string, string>();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(BossesInRaid.Count);
            foreach (var kvp in BossesInRaid)
            {
                writer.Put(kvp.Key);
                writer.Put(kvp.Value);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            int count = reader.GetInt();
            BossesInRaid = new Dictionary<string, string>(count);
            for (int i = 0; i < count; i++)
            {
                string key = reader.GetString();
                string value = reader.GetString();
                BossesInRaid[key] = value;
            }
        }
    }
}