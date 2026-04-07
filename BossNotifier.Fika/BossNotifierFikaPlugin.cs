// BossNotifierFikaPlugin.cs

using BepInEx;
using BepInEx.Logging;
using BossNotifier.Fika.Packets;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.LiteNetLib.Utils;
using System.Collections;
using UnityEngine;

namespace BossNotifier.Fika
{
    [BepInPlugin("Mattexe.BossNotifier.Fika", "BossNotifier - Fika Sync", "1.0.1")]
    [BepInDependency("Mattexe.BossNotifier")]
    [BepInDependency("com.fika.core")]
    public class BossNotifierFikaPlugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private IFikaNetworkManager networkManager;

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
            networkManager = args.Manager;

            networkManager.RegisterPacket<AllBossesPacket>(OnAllBossesReceived);
            networkManager.RegisterPacket<BossDeathPacket>(OnBossDeathReceived);
            networkManager.RegisterPacket<RequestBossesPacket>(OnRequestBossesReceived);

            LogSource.LogInfo("Fika packets registered!");
        }

        private void OnRaidStarted()
        {
            if (networkManager == null) return;

            if (FikaBackendUtils.IsServer)
            {
                // Host waits until bosses are populated
                StartCoroutine(SendBossesWhenReady());
            }
            else
            {
                // Client requests boss data
                var requestPacket = new RequestBossesPacket();
                networkManager.SendData(ref requestPacket, DeliveryMethod.ReliableOrdered, true);

                LogSource.LogInfo("Requested boss data from host");
            }
        }

        private IEnumerator SendBossesWhenReady()
        {
            yield return new WaitUntil(() => BossLocationSpawnPatch.bossesInRaid.Count > 0);

            var packet = new AllBossesPacket(BossLocationSpawnPatch.bossesInRaid);

            if (networkManager != null)
            {
                networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                LogSource.LogInfo($"Sent AllBossesPacket with {BossLocationSpawnPatch.bossesInRaid.Count} bosses (delayed)");
            }
        }

        private void OnRequestBossesReceived(RequestBossesPacket packet)
        {
            // Only host responds
            if (!FikaBackendUtils.IsServer) return;

            if (BossLocationSpawnPatch.bossesInRaid.Count == 0) return;

            var responsePacket = new AllBossesPacket(BossLocationSpawnPatch.bossesInRaid);

            if (networkManager != null)
            {
                networkManager.SendData(ref responsePacket, DeliveryMethod.ReliableOrdered, true);
                LogSource.LogInfo("Responded to boss data request");
            }
        }

        private void OnBossDied(string bossName)
        {
            // Only host sends death notifications
            if (!FikaBackendUtils.IsServer) return;

            var packet = new BossDeathPacket(bossName);

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

            BossLocationSpawnPatch.bossesInRaid.Clear();
            foreach (var kvp in packet.BossesInRaid)
            {
                BossLocationSpawnPatch.bossesInRaid[kvp.Key] = kvp.Value;
            }

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

            BotBossPatch.deadBosses.Add(packet.BossName);

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


    // --------------------
    // Request Packet
    // --------------------

    public class RequestBossesPacket : INetSerializable
    {
        public void Serialize(NetDataWriter writer) { }

        public void Deserialize(NetDataReader reader) { }
    }
}