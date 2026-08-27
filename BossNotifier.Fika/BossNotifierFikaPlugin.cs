using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core;
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

            BossNotifierPlugin.OnRaidStarted += OnRaidStarted;
            BossNotifierPlugin.OnBossDied += OnBossDied;
            BossNotifierPlugin.OnRaidEnded += OnRaidEnded;

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent args)
        {
            networkManager = args.Manager;

            networkManager.RegisterPacket<AllBossesPacket>(OnAllBossesReceived);
            networkManager.RegisterPacket<BossDeathPacket>(OnBossDeathReceived);
            networkManager.RegisterPacket<RequestBossesPacket>(OnRequestBossesReceived);
        }

        private void OnRaidStarted()
        {
            if (networkManager == null) return;

            if (FikaBackendUtils.IsServer)
                StartCoroutine(SendBossesWhenReady());
            else
            {
                var requestPacket = new RequestBossesPacket();
                networkManager.SendData(ref requestPacket, DeliveryMethod.ReliableOrdered);
            }
        }

        private IEnumerator SendBossesWhenReady()
        {
            yield return new WaitUntil(() => BossLocationSpawnPatch.bossesInRaid.Count > 0);

            var packet = new AllBossesPacket(BossLocationSpawnPatch.bossesInRaid);
            networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }

        private void OnRequestBossesReceived(RequestBossesPacket packet)
        {
            if (!FikaBackendUtils.IsServer) return;
            if (BossLocationSpawnPatch.bossesInRaid.Count == 0) return;

            var responsePacket = new AllBossesPacket(BossLocationSpawnPatch.bossesInRaid);
            networkManager.SendData(ref responsePacket, DeliveryMethod.ReliableOrdered);
        }

        private void OnBossDied(string bossName)
        {
            if (!FikaBackendUtils.IsServer) return;

            var packet = new BossDeathPacket(bossName);
            networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }

        private void OnAllBossesReceived(AllBossesPacket packet)
        {
            if (FikaBackendUtils.IsServer) return;

            BossLocationSpawnPatch.bossesInRaid.Clear();
            foreach (var kvp in packet.BossesInRaid)
                BossLocationSpawnPatch.bossesInRaid[kvp.Key] = kvp.Value;

            BossNotifierMono.Instance?.GenerateBossNotifications();
        }

        private void OnBossDeathReceived(BossDeathPacket packet)
        {
            if (FikaBackendUtils.IsServer) return;

            BotBossPatch.deadBosses.Add(packet.BossName);
            BossNotifierMono.Instance?.GenerateBossNotifications();
        }

        private void OnRaidEnded() { }
    }

    public struct RequestBossesPacket : INetSerializable
    {
        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }
}
