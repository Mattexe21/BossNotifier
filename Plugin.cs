using BepInEx;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System.Reflection;
using UnityEngine;
using EFT.Communications;
using EFT;
using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using BepInEx.Logging;
using System.Text;
using System;
using HarmonyLib;
using System.Linq;


#pragma warning disable IDE0051 // Remove unused private members

namespace BossNotifier
{
    [BepInPlugin("Mattexe.BossNotifier", "BossNotifier", "1.2.1")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class BossNotifierPlugin : BaseUnityPlugin
    {
        public static FieldInfo FikaIsPlayerHost;

        // Configuration entries - General
        public static ConfigEntry<KeyboardShortcut> showBossesKeyCode;
        public static ConfigEntry<bool> showNotificationsOnRaidStart;

        // Configuration entries - Intel Center
        public static ConfigEntry<int> intelCenterUnlockLevel;
        public static ConfigEntry<int> intelCenterLocationUnlockLevel;
        public static ConfigEntry<int> intelCenterDetectedUnlockLevel;

        // Configuration entries - Markers
        public static ConfigEntry<bool> enableMarkers;
        public static ConfigEntry<KeyboardShortcut> toggleMarkersKey;
        public static ConfigEntry<bool> showThroughWalls;
        public static ConfigEntry<string> markerCharacter;
        public static ConfigEntry<bool> showBossName;
        public static ConfigEntry<bool> showDistance;
        public static ConfigEntry<int> fontSize;
        public static ConfigEntry<float> markerBaseScale;
        public static ConfigEntry<float> markerMaxScale;
        public static ConfigEntry<int> visibilityDistance;
        public static ConfigEntry<Color> markerColor;

        private static ManualLogSource logger;

        public static event Action OnPluginAwake;
        public static event Action OnRaidStarted;
        public static event Action<string> OnBossDied;
        public static event Action OnRaidEnded;

        // Methods to invoke events (called from BossNotifierMono)
        public static void InvokeOnRaidStarted() => OnRaidStarted?.Invoke();
        public static void InvokeOnBossDied(string bossName) => OnBossDied?.Invoke(bossName);
        public static void InvokeOnRaidEnded() => OnRaidEnded?.Invoke();

        // Logging methods
        public static void Log(LogLevel level, string msg)
        {
            logger.Log(level, msg);
        }

        // Dictionary mapping boss types to names
        public static readonly Dictionary<WildSpawnType, string> bossNames = new Dictionary<WildSpawnType, string>() {
            { WildSpawnType.bossBully, "Reshala" },
            { WildSpawnType.bossKnight, "Knight" },
            { WildSpawnType.followerBigPipe, "Big Pipe" },
            { WildSpawnType.followerBirdEye, "Birdeye" },
            { WildSpawnType.sectantPriest, "Cultists" },
            { WildSpawnType.bossTagilla, "Tagilla" },
            { WildSpawnType.bossKilla, "Killa" },
            { WildSpawnType.bossZryachiy, "Zryachiy" },
            { WildSpawnType.bossGluhar, "Glukhar" },
            { WildSpawnType.bossSanitar, "Sanitar" },
            { WildSpawnType.bossKojaniy, "Shturman" },
            { WildSpawnType.bossBoar, "Kaban" },
            { WildSpawnType.gifter, "Santa Claus" },
            { WildSpawnType.arenaFighterEvent, "Blood Hounds" },
            { WildSpawnType.crazyAssaultEvent, "Crazy Scavs" },
            { WildSpawnType.exUsec, "Rogues" },
            { WildSpawnType.bossKolontay, "Kollontay" },
            { WildSpawnType.bossPartisan, "Partisan" },
            { (WildSpawnType)4206927, "Punisher" },
            { (WildSpawnType)199, "Legion" },
        };

        // Set of plural boss names
        public static readonly HashSet<string> pluralBosses = new HashSet<string>() {
            "Goons",
            "Cultists",
            "Blood Hounds",
            "Crazy Scavs",
            "Rogues",
        };

        // Goons members (for grouping)
        public static readonly HashSet<string> goonMembers = new HashSet<string>() {
            "Knight",
            "Big Pipe",
            "Birdeye",
        };

        // Dictionary mapping zone IDs to names
        public static readonly Dictionary<string, string> zoneNames = new Dictionary<string, string>() {
            // === CUSTOMS ===
            {"ZoneBrige", "Bridge" },
            {"ZoneCrossRoad", "Crossroads" },
            {"ZoneDormitory", "Dormitory" },
            {"ZoneGasStation", "Gas Station" },
            {"ZoneFactoryCenter", "Factory Center" },
            {"ZoneFactorySide", "Factory Side" },
            {"ZoneOldAZS", "Old Gas Station" },
            {"ZoneBlockPost", "Checkpoint" },
            {"ZoneTankSquare", "Old Construction" },
            {"ZoneWade", "RUAF Roadblock" },
            {"ZoneCustoms", "Customs" },
            {"ZoneScavBase", "Scav Base" },

            // === FACTORY ===
            {"BotZone", "" },

            // === INTERCHANGE ===
            {"ZoneCenterBot", "Center Floor 2" },
            {"ZoneCenter", "Center Floor 1" },
            {"ZoneOLI", "OLI" },
            {"ZoneIDEA", "IDEA" },
            {"ZoneGoshan", "Goshan" },
            {"ZoneIDEAPark", "IDEA Parking" },
            {"ZoneOLIPark", "OLI Parking" },
            {"ZoneTrucks", "Trucks" },
            {"ZoneRoad", "Road" },
            {"ZonePowerStation", "Power Station" },

            // === LABS ===
            {"BotZoneFloor1", "Floor 1" },
            {"BotZoneFloor2", "Floor 2" },
            {"BotZoneBasement", "Basement" },
            {"BotZoneGate1", "Gate 1" },
            {"BotZoneGate2", "Gate 2" },

            // === LIGHTHOUSE ===
            {"Zone_Containers", "Containers" },
            {"Zone_Rocks", "Rocks" },
            {"Zone_Chalet", "Chalet" },
            {"Zone_Village", "Village" },
            {"Zone_Bridge", "Bridge" },
            {"Zone_OldHouse", "Old House" },
            {"Zone_LongRoad", "Long Road" },
            {"Zone_RoofBeach", "Roof Beach" },
            {"Zone_DestroyedHouse", "Destroyed House" },
            {"Zone_RoofContainers", "Roof Containers" },
            {"Zone_Blockpost", "Checkpoint" },
            {"Zone_RoofRocks", "Roof Rocks" },
            {"Zone_TreatmentRocks", "Treatment Rocks" },
            {"Zone_TreatmentContainers", "Treatment Containers" },
            {"Zone_TreatmentBeach", "Treatment Beach" },
            {"Zone_Hellicopter", "Helicopter" },
            {"Zone_SniperPeak", "Sniper Peak" },
            {"Zone_Island", "Island" },

            // === RESERVE ===
            {"ZoneRailStrorage", "Rail Storage" },
            {"ZonePTOR1", "Black Pawn" },
            {"ZonePTOR2", "White Knight" },
            {"ZoneBarrack", "Barracks" },
            {"ZoneBunkerStorage", "Bunker Storage" },
            {"ZoneSubStorage", "Sub Storage" },
            {"ZoneSubCommand", "Sub Command" },

            // === GROUND ZERO ===
            {"ZoneSandbox", "" },

            // === SHORELINE ===
            {"ZoneGreenHouses", "Green Houses" },
            {"ZoneIsland", "Island" },
            {"ZoneForestGasStation", "Forest Gas Station" },
            {"ZoneBunker", "Bunker" },
            {"ZoneBusStation", "Bus Station" },
            {"ZonePort", "Pier" },
            {"ZoneForestTruck", "Forest Truck" },
            {"ZoneForestSpawn", "Forest" },
            {"ZoneSanatorium1", "Sanatorium West" },
            {"ZoneSanatorium2", "Sanatorium East" },
            {"ZoneStartVillage", "Village" },
            {"ZoneMeteoStation", "Weather Station" },
            {"ZoneRailWays", "Railways" },
            {"ZoneSmuglers", "Smugglers" },
            {"ZonePassClose", "Pass" },
            {"ZoneTunnel", "Tunnel" },

            // === STREETS ===
            {"ZoneSW01", "SW01" },
            {"ZoneConstruction", "Construction" },
            {"ZoneCarShowroom", "Car Showroom" },
            {"ZoneCinema", "Cinema" },
            {"ZoneFactory", "Factory" },
            {"ZoneHotel_1", "Hotel 1" },
            {"ZoneHotel_2", "Hotel 2" },
            {"ZoneConcordia_1", "Concordia" },
            {"ZoneConcordiaParking", "Concordia Parking" },
            {"ZoneColumn", "Column" },
            {"ZoneSW00", "SW00" },
            {"ZoneStilo", "Stilo" },
            {"ZoneCard1", "Cardinal" },
            {"ZoneMvd", "MVD" },
            {"ZoneClimova", "Klimova" },

            // === WOODS ===
            {"ZoneWoodCutter", "Wood Cutter" },
            {"ZoneHouse", "House" },
            {"ZoneBigRocks", "Big Rocks" },
            {"ZoneHighRocks", "High Rocks" },
            {"ZoneMiniHouse", "Mini House" },
            {"ZoneRedHouse", "Red House" },
            {"ZoneScavBase2", "Scav Base" },
            {"ZoneClearVill", "Village" },
            {"ZoneBrokenVill", "Broken Village" },
            {"ZoneUsecBase", "USEC Base" },
            {"ZoneStoneBunker", "Stone Bunker" },
            {"ZoneDepo", "Depot" },
        };

        private void Awake()
        {
            logger = Logger;

            Type FikaUtilExternalType = Type.GetType("Fika.Core.Coop.Utils.FikaBackendUtils, Fika.Core", false);
            if (FikaUtilExternalType != null)
            {
                FikaIsPlayerHost = AccessTools.Field(FikaUtilExternalType, "MatchingType");
            }

            // Initialize configuration entries - General
            showBossesKeyCode = Config.Bind("1. General", "Keyboard Shortcut", new KeyboardShortcut(KeyCode.O), "Key to show boss notifications.");
            showNotificationsOnRaidStart = Config.Bind("1. General", "Show Bosses on Raid Start", true, "Show boss notifications on raid start.");

            // Initialize configuration entries - Intel Center
            intelCenterUnlockLevel = Config.Bind("2. Intel Center Unlocks (4 means Disabled)", "1. Intel Center Level Requirement", 0,
                new ConfigDescription("Level to unlock plain notifications at.",
                new AcceptableValueRange<int>(0, 4)));

            intelCenterLocationUnlockLevel = Config.Bind("2. Intel Center Unlocks (4 means Disabled)", "2. Intel Center Location Level Requirement", 0,
                new ConfigDescription("Unlocks showing boss spawn location in notification.",
                new AcceptableValueRange<int>(0, 4)));

            intelCenterDetectedUnlockLevel = Config.Bind("2. Intel Center Unlocks (4 means Disabled)", "3. Intel Center Detection Requirement", 0,
                new ConfigDescription("Unlocks showing boss detected notification. (When you get near a boss)",
                new AcceptableValueRange<int>(0, 4)));

            // Initialize configuration entries - Markers
            enableMarkers = Config.Bind("3. Boss Markers", "1. Enable Markers", true, "Show 3D markers above boss heads.");

            toggleMarkersKey = Config.Bind("3. Boss Markers", "2. Toggle Markers Key", new KeyboardShortcut(KeyCode.P), "Key to toggle marker visibility.");

            showThroughWalls = Config.Bind("3. Boss Markers", "3. Show Through Walls", true, "If enabled, markers are visible through walls. If disabled, markers only show when you have line of sight to the boss.");

            markerCharacter = Config.Bind("3. Boss Markers", "4. Marker Character", "▼",
                new ConfigDescription("Character to display as marker.",
                new AcceptableValueList<string>("▼", "▽", "↓", "●", "◆", "★", "☠", "◉", "▲")));

            showBossName = Config.Bind("3. Boss Markers", "5. Show Boss Name", true, "Show boss name on marker.");

            showDistance = Config.Bind("3. Boss Markers", "6. Show Distance", true, "Show distance to boss on marker.");

            fontSize = Config.Bind("3. Boss Markers", "7. Marker Size", 64,
                new ConfigDescription("Font size of the marker.",
                new AcceptableValueRange<int>(32, 128)));

            markerBaseScale = Config.Bind("3. Boss Markers", "8. Base Scale", 0.04f,
                new ConfigDescription("Base scale of markers.",
                new AcceptableValueRange<float>(0.01f, 0.2f)));

            markerMaxScale = Config.Bind("3. Boss Markers", "9. Max Scale", 0.3f,
                new ConfigDescription("Maximum scale at far distances.",
                new AcceptableValueRange<float>(0.1f, 1f)));

            visibilityDistance = Config.Bind("3. Boss Markers", "10. Visibility Distance", 200,
                new ConfigDescription("Maximum distance (meters) to show markers.",
                new AcceptableValueRange<int>(50, 500)));

            markerColor = Config.Bind("3. Boss Markers", "11. Marker Color", new Color(1f, 0f, 0f, 1f), "Color of boss markers (red by default).");

            // Enable patches
            new BossLocationSpawnPatch().Enable();
            new NewGamePatch().Enable();
            new BotBossPatch().Enable();

            // Subscribe to config changes
            Config.SettingChanged += Config_SettingChanged;

            Logger.LogInfo($"Plugin BossNotifier v1.2.1 is loaded!");

            // Invoke event for addon to hook into
            OnPluginAwake?.Invoke();
        }

        // Event handler for configuration changes
        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            ConfigEntryBase changedSetting = e.ChangedSetting;

            // If player is in a raid, reset their notifications to reflect changes
            if (BossNotifierMono.Instance) BossNotifierMono.Instance.GenerateBossNotifications();
        }

        // Get boss name by type
        public static string GetBossName(WildSpawnType type)
        {
            return bossNames.ContainsKey(type) ? bossNames[type] : null;
        }

        // Check if a WildSpawnType is a boss
        public static bool IsBoss(WildSpawnType type)
        {
            return bossNames.ContainsKey(type);
        }

        // Get zone name by ID
        public static string GetZoneName(string zoneId)
        {
            if (zoneNames.ContainsKey(zoneId)) return zoneNames[zoneId];

            string location = zoneId.Replace("Bot", "").Replace("Zone", "");
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < location.Length; i++)
            {
                char c = location[i];
                if (char.IsUpper(c) && i != 0 && i < location.Length - 1 && !char.IsUpper(location[i + 1]) && !char.IsDigit(location[i + 1]))
                {
                    sb.Append(" ");
                }
                sb.Append(c);
            }
            return sb.ToString().Replace("_", " ").Trim();
        }
    }

    #region Patches
    // Patch for tracking boss location spawns
    internal class BossLocationSpawnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BossLocationSpawn).GetMethod("Init");

        public static Dictionary<string, string> bossesInRaid = new Dictionary<string, string>();

        private static void TryAddBoss(string boss, string location)
        {
            if (location == null)
            {
                Logger.LogError("Tried to add boss with null location.");
                return;
            }
            if (bossesInRaid.ContainsKey(boss))
            {
                if (!bossesInRaid[boss].Contains(location) && !location.Equals(""))
                {
                    if (bossesInRaid[boss].Equals(""))
                    {
                        bossesInRaid[boss] = location;
                    }
                    else
                    {
                        bossesInRaid[boss] += ", " + location;
                    }
                }
            }
            else
            {
                bossesInRaid.Add(boss, location);
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(BossLocationSpawn __instance)
        {
            if (__instance.ShallSpawn)
            {
                string name = BossNotifierPlugin.GetBossName(__instance.BossType);
                if (name == null) return;

                string location = BossNotifierPlugin.GetZoneName(__instance.BornZone);

                BossNotifierPlugin.Log(LogLevel.Info, $"Boss {name} @ zone {__instance.BornZone} translated to {(location == null ? __instance.BornZone.Replace("Bot", "").Replace("Zone", "") : location)}");

                if (location == null)
                {
                    TryAddBoss(name, __instance.BornZone.Replace("Bot", "").Replace("Zone", ""));
                }
                else if (location.Equals(""))
                {
                    TryAddBoss(name, "");
                }
                else
                {
                    TryAddBoss(name, location);
                }
            }
        }
    }

    // Patch for tracking live boss spawns
    internal class BotBossPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(BotBoss).GetConstructors()[0];

        public static HashSet<string> spawnedBosses = new HashSet<string>();
        public static HashSet<string> deadBosses = new HashSet<string>();
        public static HashSet<string> vicinityNotificationsSent = new HashSet<string>(); // FIX: Track sent notifications
        public static Queue<string> vicinityNotifications = new Queue<string>();

        [PatchPostfix]
        private static void PatchPostfix(BotBoss __instance)
        {
            WildSpawnType role = __instance.Owner.Profile.Info.Settings.Role;
            string name = BossNotifierPlugin.GetBossName(role);
            if (name == null) return;

            Vector3 positionVector = __instance.Player().Position;
            string position = $"{(int)positionVector.x}, {(int)positionVector.y}, {(int)positionVector.z}";
            BossNotifierPlugin.Log(LogLevel.Info, $"{name} has spawned at {position} on {Singleton<GameWorld>.Instance.LocationId}");

            spawnedBosses.Add(name);

            // Use "Goons" for vicinity notification if it's a goon member
            string notifName = BossNotifierPlugin.goonMembers.Contains(name) ? "Goons" : name;

            // FIX: Only send one vicinity notification per boss/group
            if (!vicinityNotificationsSent.Contains(notifName))
            {
                vicinityNotificationsSent.Add(notifName);
                bool isPlural = BossNotifierPlugin.pluralBosses.Contains(notifName);
                vicinityNotifications.Enqueue($"{notifName} {(isPlural ? "have" : "has")} been detected in your vicinity.");
            }
        }
    }

    // Patch for hooking when a raid is started
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod("OnGameStarted");

        [PatchPrefix]
        public static void PatchPrefix()
        {
            BossNotifierMono.Init();
        }
    }
    #endregion

    #region Billboard Component
    // Billboard component - makes object always face the camera
    public class Billboard : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }
        }
    }
    #endregion

    #region BossMarkerInfo
    // Class to hold marker information
    public class BossMarkerInfo
    {
        public Player Player { get; set; }
        public string BossName { get; set; }
        public GameObject MarkerObject { get; set; }
        public TextMesh SymbolTextMesh { get; set; }
        public TextMesh InfoTextMesh { get; set; }

        public BossMarkerInfo(Player player, string bossName, GameObject markerObject, TextMesh symbolTextMesh, TextMesh infoTextMesh)
        {
            Player = player;
            BossName = bossName;
            MarkerObject = markerObject;
            SymbolTextMesh = symbolTextMesh;
            InfoTextMesh = infoTextMesh;
        }
    }
    #endregion

    #region BossNotifierMono
    class BossNotifierMono : MonoBehaviour
    {
        public static BossNotifierMono Instance;
        private List<string> bossNotificationMessages;
        public int intelCenterLevel;

        // Marker system
        private GameWorld _gameWorld;
        private Dictionary<Player, BossMarkerInfo> _bossMarkers = new Dictionary<Player, BossMarkerInfo>();
        private bool _markersVisible = true;
        private Camera _mainCamera;

        #region Initialization
        public static void Init()
        {
            if (Singleton<GameWorld>.Instantiated)
            {
                Instance = Singleton<GameWorld>.Instance.GetOrAddComponent<BossNotifierMono>();
                BossNotifierPlugin.Log(LogLevel.Info, $"Game started on map {Singleton<GameWorld>.Instance.LocationId}");

                // Get Intel Center level - SPT 4.0.x compatible
                try
                {
                    var session = ClientAppUtils.GetMainApp().GetClientBackEndSession();
                    var areas = session.Profile.Hideout.Areas;

                    if (areas != null)
                    {
                        var intelCenter = areas.FirstOrDefault(a => a.AreaType == EAreaType.IntelligenceCenter);
                        Instance.intelCenterLevel = intelCenter?.Level ?? 0;
                    }
                    else
                    {
                        Instance.intelCenterLevel = 0;
                    }
                }
                catch (Exception ex)
                {
                    BossNotifierPlugin.Log(LogLevel.Warning, $"Could not get Intel Center level: {ex.Message}");
                    Instance.intelCenterLevel = 0;
                }

                BossNotifierPlugin.Log(LogLevel.Info, $"Intel Center level: {Instance.intelCenterLevel}");
            }
        }

        public void Start()
        {
            _gameWorld = Singleton<GameWorld>.Instance;
            _mainCamera = Camera.main;
            _markersVisible = BossNotifierPlugin.enableMarkers.Value;

            // Register for player spawn events
            if (_gameWorld != null)
            {
                _gameWorld.OnPersonAdd += OnPersonAdd;
                BossNotifierPlugin.Log(LogLevel.Info, "Registered OnPersonAdd event");
            }

            // Initialize markers for already spawned players (bosses)
            InitializeExistingBossMarkers();

            GenerateBossNotifications();

            // Notify addon that raid has started
            BossNotifierPlugin.InvokeOnRaidStarted();

            if (!BossNotifierPlugin.showNotificationsOnRaidStart.Value) return;
            Invoke("SendBossNotifications", 2f);
        }

        private void InitializeExistingBossMarkers()
        {
            if (_gameWorld == null || _gameWorld.AllAlivePlayersList == null) return;

            foreach (var player in _gameWorld.AllAlivePlayersList)
            {
                if (player == null || player.IsYourPlayer) continue;

                var role = player.Profile?.Info?.Settings?.Role;
                if (role.HasValue && BossNotifierPlugin.IsBoss(role.Value))
                {
                    string bossName = BossNotifierPlugin.GetBossName(role.Value);
                    if (bossName != null && !_bossMarkers.ContainsKey(player))
                    {
                        CreateBossMarker(player, bossName);

                        // FIX: Subscribe to death event for existing bosses
                        player.OnPlayerDeadOrUnspawn += OnBossDeadOrUnspawn;
                    }
                }
            }
        }
        #endregion

        #region Marker Creation
        private void OnPersonAdd(IPlayer iPlayer)
        {
            Player player = iPlayer as Player;
            if (player == null || player.IsYourPlayer) return;

            var role = player.Profile?.Info?.Settings?.Role;
            if (!role.HasValue) return;

            // Check if this is a boss
            if (BossNotifierPlugin.IsBoss(role.Value))
            {
                string bossName = BossNotifierPlugin.GetBossName(role.Value);
                if (bossName != null)
                {
                    BossNotifierPlugin.Log(LogLevel.Info, $"Boss detected via OnPersonAdd: {bossName}");
                    CreateBossMarker(player, bossName);

                    // Subscribe to death event
                    player.OnPlayerDeadOrUnspawn += OnBossDeadOrUnspawn;
                }
            }
        }

        private void CreateBossMarker(Player player, string bossName)
        {
            if (_bossMarkers.ContainsKey(player)) return;

            try
            {
                // Create main marker GameObject
                GameObject markerObj = new GameObject($"BossMarker_{bossName}");

                // Create symbol object (▼) - child of marker
                GameObject symbolObj = new GameObject("Symbol");
                symbolObj.transform.SetParent(markerObj.transform);
                symbolObj.transform.localPosition = Vector3.zero;

                TextMesh symbolMesh = symbolObj.AddComponent<TextMesh>();
                symbolMesh.text = BossNotifierPlugin.markerCharacter.Value;
                symbolMesh.fontSize = BossNotifierPlugin.fontSize.Value;
                symbolMesh.anchor = TextAnchor.MiddleCenter;
                symbolMesh.alignment = TextAlignment.Center;
                symbolMesh.color = BossNotifierPlugin.markerColor.Value;
                symbolMesh.fontStyle = FontStyle.Bold;

                // Create info object (name + distance) - child of marker
                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(markerObj.transform);
                infoObj.transform.localPosition = new Vector3(0, -0.015f, 0);

                TextMesh infoMesh = infoObj.AddComponent<TextMesh>();
                infoMesh.text = bossName;
                infoMesh.fontSize = 48;
                infoMesh.anchor = TextAnchor.UpperCenter;
                infoMesh.alignment = TextAlignment.Center;
                infoMesh.color = BossNotifierPlugin.markerColor.Value;
                infoMesh.fontStyle = FontStyle.Bold;

                // Add Billboard component to main marker (children follow)
                markerObj.AddComponent<Billboard>();

                // Set initial scale
                markerObj.transform.localScale = Vector3.one * BossNotifierPlugin.markerBaseScale.Value;

                // Position above head
                UpdateMarkerPosition(markerObj, player);

                // Set active based on config
                markerObj.SetActive(_markersVisible && BossNotifierPlugin.enableMarkers.Value);

                // Store marker info
                var markerInfo = new BossMarkerInfo(player, bossName, markerObj, symbolMesh, infoMesh);
                _bossMarkers[player] = markerInfo;

                BossNotifierPlugin.Log(LogLevel.Info, $"Created marker for boss: {bossName}");
            }
            catch (Exception ex)
            {
                BossNotifierPlugin.Log(LogLevel.Error, $"Failed to create marker for {bossName}: {ex.Message}");
            }
        }

        private void OnBossDeadOrUnspawn(Player player)
        {
            if (player == null) return;

            player.OnPlayerDeadOrUnspawn -= OnBossDeadOrUnspawn;

            if (_bossMarkers.TryGetValue(player, out var markerInfo))
            {
                BossNotifierPlugin.Log(LogLevel.Info, $"Boss died/unspawned: {markerInfo.BossName}");

                // Track dead boss
                BotBossPatch.deadBosses.Add(markerInfo.BossName);

                // Notify addon of boss death
                BossNotifierPlugin.InvokeOnBossDied(markerInfo.BossName);

                if (markerInfo.MarkerObject != null)
                {
                    Destroy(markerInfo.MarkerObject);
                }
                _bossMarkers.Remove(player);

                // FIX: Regenerate notifications after boss death
                GenerateBossNotifications();
            }
        }
        #endregion

        #region Update Loop
        public void Update()
        {
            // Handle vicinity notifications
            if (BotBossPatch.vicinityNotifications.Count > 0)
            {
                string notif = BotBossPatch.vicinityNotifications.Dequeue();
                if (Instance.intelCenterLevel >= BossNotifierPlugin.intelCenterDetectedUnlockLevel.Value)
                {
                    NotificationManagerClass.DisplayMessageNotification(notif, ENotificationDurationType.Long);
                    Instance.GenerateBossNotifications();
                }
            }

            // Handle keyboard shortcuts
            if (IsKeyPressed(BossNotifierPlugin.showBossesKeyCode.Value))
            {
                SendBossNotifications();
            }

            // Handle marker toggle key
            if (IsKeyPressed(BossNotifierPlugin.toggleMarkersKey.Value))
            {
                _markersVisible = !_markersVisible;
                ToggleAllMarkers(_markersVisible);
                string status = _markersVisible ? "ON" : "OFF";
                NotificationManagerClass.DisplayMessageNotification($"Boss Markers: {status}", ENotificationDurationType.Default);
            }

            // Update markers
            if (BossNotifierPlugin.enableMarkers.Value && _markersVisible)
            {
                UpdateAllMarkers();
            }
        }

        private void UpdateAllMarkers()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Vector3 playerPos = _mainCamera.transform.position;
            List<Player> toRemove = new List<Player>();

            foreach (var kvp in _bossMarkers)
            {
                var player = kvp.Key;
                var markerInfo = kvp.Value;

                // Check if player/marker still valid
                if (player == null || markerInfo.MarkerObject == null)
                {
                    toRemove.Add(player);
                    continue;
                }

                // Check if player is dead
                if (player.HealthController != null && !player.HealthController.IsAlive)
                {
                    toRemove.Add(player);
                    continue;
                }

                // Calculate distance
                float distance = Vector3.Distance(playerPos, player.Position);

                // Hide if too far
                if (distance > BossNotifierPlugin.visibilityDistance.Value)
                {
                    markerInfo.MarkerObject.SetActive(false);
                    continue;
                }

                // Line of sight check (if show through walls is disabled)
                if (!BossNotifierPlugin.showThroughWalls.Value)
                {
                    Vector3 bossHeadPos = player.Position + Vector3.up * 1.5f;
                    Vector3 direction = bossHeadPos - playerPos;

                    int layerMask = (1 << 12) | (1 << 11) | (1 << 16);

                    if (Physics.Raycast(playerPos, direction.normalized, out RaycastHit hit, distance, layerMask))
                    {
                        if (hit.distance < distance - 1f)
                        {
                            markerInfo.MarkerObject.SetActive(false);
                            continue;
                        }
                    }
                }

                // Show marker
                markerInfo.MarkerObject.SetActive(true);

                // Update position
                UpdateMarkerPosition(markerInfo.MarkerObject, player);

                // Update scale based on distance
                UpdateMarkerScale(markerInfo.MarkerObject, distance);

                // Update text (with distance if enabled)
                UpdateMarkerText(markerInfo, distance);
            }

            // Clean up dead/invalid markers
            foreach (var player in toRemove)
            {
                if (_bossMarkers.TryGetValue(player, out var markerInfo))
                {
                    if (markerInfo.MarkerObject != null)
                    {
                        Destroy(markerInfo.MarkerObject);
                    }
                    _bossMarkers.Remove(player);
                }
            }
        }

        private void UpdateMarkerPosition(GameObject marker, Player player)
        {
            if (player == null || marker == null) return;

            Vector3 headPos = player.Position + Vector3.up * 2.2f;
            marker.transform.position = headPos;
        }

        private void UpdateMarkerScale(GameObject marker, float distance)
        {
            float baseScale = BossNotifierPlugin.markerBaseScale.Value;
            float maxScale = BossNotifierPlugin.markerMaxScale.Value;

            float scale = baseScale * (1f + distance / 50f);
            scale = Mathf.Clamp(scale, baseScale, maxScale);

            marker.transform.localScale = Vector3.one * scale;
        }

        private void UpdateMarkerText(BossMarkerInfo markerInfo, float distance)
        {
            // Update symbol
            if (markerInfo.SymbolTextMesh != null)
            {
                markerInfo.SymbolTextMesh.text = BossNotifierPlugin.markerCharacter.Value;
                markerInfo.SymbolTextMesh.fontSize = BossNotifierPlugin.fontSize.Value;
                markerInfo.SymbolTextMesh.color = BossNotifierPlugin.markerColor.Value;
            }

            // Update info (name + distance)
            if (markerInfo.InfoTextMesh != null)
            {
                bool showName = BossNotifierPlugin.showBossName.Value;
                bool showDist = BossNotifierPlugin.showDistance.Value;

                if (showName && showDist)
                {
                    markerInfo.InfoTextMesh.text = $"{markerInfo.BossName}\n{distance:F0}m";
                }
                else if (showName && !showDist)
                {
                    markerInfo.InfoTextMesh.text = markerInfo.BossName;
                }
                else if (!showName && showDist)
                {
                    markerInfo.InfoTextMesh.text = $"{distance:F0}m";
                }
                else
                {
                    markerInfo.InfoTextMesh.text = "";
                }

                markerInfo.InfoTextMesh.color = BossNotifierPlugin.markerColor.Value;

                float fontSizeRatio = BossNotifierPlugin.fontSize.Value / 64f;
                float yOffset = -0.015f * fontSizeRatio;
                markerInfo.InfoTextMesh.transform.localPosition = new Vector3(0, yOffset, 0);
            }
        }

        private void ToggleAllMarkers(bool visible)
        {
            foreach (var markerInfo in _bossMarkers.Values)
            {
                if (markerInfo.MarkerObject != null)
                {
                    markerInfo.MarkerObject.SetActive(visible);
                }
            }
        }
        #endregion

        #region Notifications
        private void SendBossNotifications()
        {
            if (!ShouldFunction()) return;
            if (intelCenterLevel < BossNotifierPlugin.intelCenterUnlockLevel.Value) return;

            if (bossNotificationMessages.Count == 0)
            {
                NotificationManagerClass.DisplayMessageNotification("No Bosses Located", ENotificationDurationType.Long);
                return;
            }

            foreach (var bossMessage in bossNotificationMessages)
            {
                NotificationManagerClass.DisplayMessageNotification(bossMessage, ENotificationDurationType.Long);
            }
        }

        public void GenerateBossNotifications()
        {
            bossNotificationMessages = new List<string>();

            bool isDayTime = IsDay();
            bool isLocationUnlocked = intelCenterLevel >= BossNotifierPlugin.intelCenterLocationUnlockLevel.Value;
            bool isDetectionUnlocked = intelCenterLevel >= BossNotifierPlugin.intelCenterDetectedUnlockLevel.Value;

            // Track if we already added Goons notification
            bool goonsNotificationAdded = false;
            string goonsLocation = "";
            bool goonsDetected = false;

            foreach (var bossSpawn in BossLocationSpawnPatch.bossesInRaid)
            {
                if (isDayTime && bossSpawn.Key.Equals("Cultists")) continue;

                // Handle Goons as a group
                if (BossNotifierPlugin.goonMembers.Contains(bossSpawn.Key))
                {
                    if (!goonsNotificationAdded)
                    {
                        goonsLocation = bossSpawn.Value;
                        goonsDetected = BotBossPatch.spawnedBosses.Contains("Knight") ||
                                       BotBossPatch.spawnedBosses.Contains("Big Pipe") ||
                                       BotBossPatch.spawnedBosses.Contains("Birdeye");

                        // Check if ALL goons are dead
                        bool allGoonsDead = BotBossPatch.deadBosses.Contains("Knight") &&
                                           BotBossPatch.deadBosses.Contains("Big Pipe") &&
                                           BotBossPatch.deadBosses.Contains("Birdeye");

                        string goonsMessage;
                        if (allGoonsDead)
                        {
                            goonsMessage = "Goons have been eliminated. ☠";
                        }
                        else if (!isLocationUnlocked || goonsLocation == null || goonsLocation.Equals(""))
                        {
                            goonsMessage = $"Goons have been located.{(isDetectionUnlocked && goonsDetected ? " ✓" : "")}";
                        }
                        else
                        {
                            goonsMessage = $"Goons have been located near {goonsLocation}.{(isDetectionUnlocked && goonsDetected ? " ✓" : "")}";
                        }
                        bossNotificationMessages.Add(goonsMessage);
                        goonsNotificationAdded = true;
                    }
                    continue;
                }

                bool isDetected = BotBossPatch.spawnedBosses.Contains(bossSpawn.Key);
                bool isDead = BotBossPatch.deadBosses.Contains(bossSpawn.Key);

                string notificationMessage;

                if (isDead)
                {
                    notificationMessage = $"{bossSpawn.Key} {(BossNotifierPlugin.pluralBosses.Contains(bossSpawn.Key) ? "have" : "has")} been eliminated. ☠";
                }
                else if (!isLocationUnlocked || bossSpawn.Value == null || bossSpawn.Value.Equals(""))
                {
                    notificationMessage = $"{bossSpawn.Key} {(BossNotifierPlugin.pluralBosses.Contains(bossSpawn.Key) ? "have" : "has")} been located.{(isDetectionUnlocked && isDetected ? " ✓" : "")}";
                }
                else
                {
                    notificationMessage = $"{bossSpawn.Key} {(BossNotifierPlugin.pluralBosses.Contains(bossSpawn.Key) ? "have" : "has")} been located near {bossSpawn.Value}.{(isDetectionUnlocked && isDetected ? " ✓" : "")}";
                }
                bossNotificationMessages.Add(notificationMessage);
            }
        }

        private bool IsDay()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                int hour = gameWorld.GameDateTime.Calculate().Hour;
                return hour >= 7 && hour < 22;
            }
            return false;
        }
        #endregion

        #region Cleanup
        public void OnDestroy()
        {
            if (_gameWorld != null)
            {
                _gameWorld.OnPersonAdd -= OnPersonAdd;
            }

            foreach (var markerInfo in _bossMarkers.Values)
            {
                if (markerInfo.MarkerObject != null)
                {
                    Destroy(markerInfo.MarkerObject);
                }
                if (markerInfo.Player != null)
                {
                    markerInfo.Player.OnPlayerDeadOrUnspawn -= OnBossDeadOrUnspawn;
                }
            }
            _bossMarkers.Clear();

            BossLocationSpawnPatch.bossesInRaid.Clear();
            BotBossPatch.spawnedBosses.Clear();
            BotBossPatch.deadBosses.Clear();
            BotBossPatch.vicinityNotificationsSent.Clear(); // FIX: Clear this too

            // Notify addon that raid ended
            BossNotifierPlugin.InvokeOnRaidEnded();
        }
        #endregion

        #region Utility
        public bool ShouldFunction()
        {
            if (BossNotifierPlugin.FikaIsPlayerHost == null) return true;
            return (int)BossNotifierPlugin.FikaIsPlayerHost.GetValue(null) == 2;
        }

        bool IsKeyPressed(KeyboardShortcut key)
        {
            if (!UnityInput.Current.GetKeyDown(key.MainKey))
            {
                return false;
            }
            foreach (var modifier in key.Modifiers)
            {
                if (!UnityInput.Current.GetKey(modifier))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
    #endregion
}