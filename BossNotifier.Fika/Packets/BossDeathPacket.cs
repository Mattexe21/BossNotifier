using Fika.Core.Networking.LiteNetLib.Utils;


namespace BossNotifier.Fika.Packets
{
    public struct BossDeathPacket : INetSerializable
    {
        public string BossName;

        public BossDeathPacket(string bossName)
        {
            BossName = bossName ?? "";
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(BossName);
        }

        public void Deserialize(NetDataReader reader)
        {
            BossName = reader.GetString();
        }
    }
}