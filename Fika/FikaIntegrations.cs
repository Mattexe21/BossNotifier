using BepInEx.Bootstrap;
using BepInEx.Logging;
using BossNotifier.Fika.Packets;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using System;
using System.Runtime.CompilerServices;

namespace BossNotifier.Fika
{
    public static class FikaIntegration
    {
        private static bool _fikaAvailable = false;
        private static bool _initialized = false;
        private static bool _packetsRegistered = false;

        public static bool IsFikaInstalled => _fikaAvailable;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                if (Chainloader.PluginInfos.ContainsKey("com.fika.core"))
                {
                    _fikaAvailable = true;
                    BossNotifierPlugin.Log(LogLevel.Info, "Fika detected - multiplayer sync enabled");

                    SubscribeToFikaEvents();
                }
                else
                {
                    _fikaAvailable = false;
                    BossNotifierPlugin.Log(LogLevel.Info, "Fika not detected - running in single player mode");
                }
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Warning, $"Error initializing Fika integration: {ex.Message}");
                _fikaAvailable = false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubscribeToFikaEvents()
        {
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent args)
        {
            try
            {
                if (_packetsRegistered) return;

                var networkManager = args.Manager;

                networkManager.RegisterPacket<AllBossesPacket>(OnAllBossesReceived);
                networkManager.RegisterPacket<BossDeathPacket>(OnBossDeathReceived);

                _packetsRegistered = true;
                BossNotifierPlugin.Log(LogLevel.Info, "Fika packets registered successfully");
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Error registering Fika packets: {ex.Message}");
            }
        }

        public static bool IsHost()
        {
            if (!_fikaAvailable) return true;

            try
            {
                return IsHostInternal();
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Warning, $"Error checking IsHost: {ex.Message}");
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsHostInternal()
        {
            return FikaBackendUtils.IsServer;
        }

        public static void OnRaidStarted()
        {
            if (!_fikaAvailable) return;

            try
            {
                OnRaidStartedInternal();
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Error in OnRaidStarted: {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnRaidStartedInternal()
        {
            if (FikaBackendUtils.IsServer)
            {
                SendAllBossesPacket();
            }
        }

        public static void OnBossDeath(string bossName)
        {
            if (!_fikaAvailable) return;

            try
            {
                OnBossDeathInternal(bossName);
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Error in OnBossDeath: {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnBossDeathInternal(string bossName)
        {
            if (FikaBackendUtils.IsServer)
            {
                SendBossDeathPacket(bossName);
            }
        }

        #region Packet Sending

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SendAllBossesPacket()
        {
            try
            {
                var networkManager = Singleton<IFikaNetworkManager>.Instance;
                if (networkManager == null)
                {
                    BossNotifierPlugin.Log(LogLevel.Warning, "Cannot send packet - NetworkManager not available");
                    return;
                }

                var packet = new AllBossesPacket(BossLocationSpawnPatch.bossesInRaid);
                networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);

                BossNotifierPlugin.Log(LogLevel.Info, $"Sent AllBossesPacket with {BossLocationSpawnPatch.bossesInRaid.Count} bosses");
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Error sending AllBossesPacket: {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SendBossDeathPacket(string bossName)
        {
            try
            {
                var networkManager = Singleton<IFikaNetworkManager>.Instance;
                if (networkManager == null) return;

                var packet = new BossDeathPacket(bossName);
                networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);

                BossNotifierPlugin.Log(LogLevel.Info, $"Sent BossDeathPacket for {bossName}");
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Error sending BossDeathPacket: {ex.Message}");
            }
        }

        #endregion

        #region Packet Receiving

        private static void OnAllBossesReceived(AllBossesPacket packet)
        {
            try
            {
                BossNotifierPlugin.Log(LogLevel.Info, $"Received AllBossesPacket with {packet.BossesInRaid.Count} bosses");

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
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Error processing AllBossesPacket: {ex.Message}");
            }
        }

        private static void OnBossDeathReceived(BossDeathPacket packet)
        {
            try
            {
                BossNotifierPlugin.Log(LogLevel.Info, $"Received BossDeathPacket for {packet.BossName}");

                BotBossPatch.deadBosses.Add(packet.BossName);

                if (BossNotifierMono.Instance != null)
                {
                    BossNotifierMono.Instance.GenerateBossNotifications();
                }
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Error processing BossDeathPacket: {ex.Message}");
            }
        }

        #endregion

        public static void Reset()
        {
            _packetsRegistered = false;
        }
    }
}