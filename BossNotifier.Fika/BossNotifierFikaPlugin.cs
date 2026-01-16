using BepInEx;
using BepInEx.Logging;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using BossNotifier.Fika.Packets;
using Comfort.Common;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Main.Utils;

namespace BossNotifier.Fika
{
    [BepInPlugin("Mattexe.BossNotifier.Fika", "BossNotifier - Fika Sync", "1.0.0")]
    [BepInDependency("Mattexe.BossNotifier")]
    [BepInDependency("com.fika.core")]
    public class BossNotifierFikaPlugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;

            // Subscribe to BossNotifier events
            BossNotifierPlugin.OnRaidStarted += OnRaidStarted;
            BossNotifierPlugin.OnBossDied += OnBossDied;
            BossNotifierPlugin.OnRaidEnded += OnRaidEnded;

            // Subscribe to Fika network events
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);

            Logger.LogInfo("BossNotifier Fika Sync loaded!");
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent args)
        {
            var networkManager = args.Manager;
            networkManager.RegisterPacket<AllBossesPacket>(OnAllBossesReceived);
            networkManager.RegisterPacket<BossDeathPacket>(OnBossDeathReceived);

            LogSource.LogInfo("Fika packets registered!");
        }

        private void OnRaidStarted()
        {
            // Only host sends boss data
            if (!FikaBackendUtils.IsServer) return;

            var bossesInRaid = BossLocationSpawnPatch.bossesInRaid;
            if (bossesInRaid.Count == 0) return;

            var packet = new AllBossesPacket(bossesInRaid);
            var networkManager = Singleton<IFikaNetworkManager>.Instance;

            if (networkManager != null)
            {
                networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                LogSource.LogInfo($"Sent AllBossesPacket with {bossesInRaid.Count} bosses");
            }
        }

        private void OnBossDied(string bossName)
        {
            // Only host sends death notifications
            if (!FikaBackendUtils.IsServer) return;

            var packet = new BossDeathPacket(bossName);
            var networkManager = Singleton<IFikaNetworkManager>.Instance;

            if (networkManager != null)
            {
                networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                LogSource.LogInfo($"Sent BossDeathPacket for {bossName}");
            }
        }

        private void OnAllBossesReceived(AllBossesPacket packet)
        {
            // Only clients process this
            if (FikaBackendUtils.IsServer) return;

            LogSource.LogInfo($"Received AllBossesPacket with {packet.BossesInRaid.Count} bosses");

            // Update local boss data
            BossLocationSpawnPatch.bossesInRaid.Clear();
            foreach (var kvp in packet.BossesInRaid)
            {
                BossLocationSpawnPatch.bossesInRaid[kvp.Key] = kvp.Value;
            }

            // Regenerate notifications
            if (BossNotifierMono.Instance != null)
            {
                BossNotifierMono.Instance.GenerateBossNotifications();
            }
        }

        private void OnBossDeathReceived(BossDeathPacket packet)
        {
            // Only clients process this
            if (FikaBackendUtils.IsServer) return;

            LogSource.LogInfo($"Received BossDeathPacket for {packet.BossName}");

            // Mark boss as dead
            BotBossPatch.deadBosses.Add(packet.BossName);

            // Regenerate notifications
            if (BossNotifierMono.Instance != null)
            {
                BossNotifierMono.Instance.GenerateBossNotifications();
            }
        }

        private void OnRaidEnded()
        {
            // Cleanup if needed
        }
    }
}