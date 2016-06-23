using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Ceto;
using Steamworks;
using TheForest.Player;
using TheForest.UI;
using TheForest.Utils;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.TheForest.Libraries.Covalence;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The core The Forest plugin
    /// </summary>
    public class TheForestCore : CSPlugin
    {
        #region Initialization

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The Forest covalence provider
        private readonly TheForestCovalenceProvider covalence = TheForestCovalenceProvider.Instance;

        // TODO: Localization of core

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the TheForestCore class
        /// </summary>
        public TheForestCore()
        {
            // Set attributes
            Name = "TheForestCore";
            Title = "The Forest";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>();
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(BoltEntity player)
        {
            if (permission.IsLoaded) return true;
            // TODO: PermissionsNotLoaded reply to player
            return false;
        }

        #endregion

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("version", TheForestExtension.GameVersion);

            // Setup the default permission groups
            if (permission.IsLoaded)
            {
                var rank = 0;
                for (var i = DefaultGroups.Length - 1; i >= 0; i--)
                {
                    var defaultGroup = DefaultGroups[i];
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
                permission.RegisterValidate(s =>
                {
                    ulong temp;
                    if (!ulong.TryParse(s, out temp)) return false;
                    var digits = temp == 0 ? 1 : (int)Math.Floor(Math.Log10(temp) + 1);
                    return digits >= 17;
                });
                permission.CleanUp();
            }
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore") InitializeLogging();
        }

        #endregion

        #region Server Hooks

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", PlayerPrefs.GetString("MpGameName"));

            // Add 'oxide' and 'modded' tags
            SteamGameServer.SetGameTags("oxide,modded");

            // Update server console window and status bars
            TheForestExtension.ServerConsole();

            // Disable audio and client-side elements if not dedicated
            if (TheForestExtension.DisableClient)
            {
                DisableAudio();
                DisableClient();
            }

            // Save the level every X minutes
            Interface.Oxide.GetLibrary<Timer>().Once(300f, () => LevelSerializer.SaveGame("Game"));
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(BoltEntity player)
        {
            var id = player.source.RemoteEndPoint.SteamId.Id;
            var name = SteamFriends.GetFriendPersonaName(new CSteamID(id));

            Debug.Log($"{id}/{name} joined");

            // Do permission stuff
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(id.ToString(), name);

                // Add player to default group
                if (!permission.UserHasGroup(id.ToString(), DefaultGroups[0])) permission.AddUserGroup(id.ToString(), DefaultGroups[0]);
            }

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerConnect(player);
            Interface.Call("OnUserConnected", covalence.PlayerManager.GetPlayer(id.ToString()));
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="connection"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(BoltConnection connection)
        {
            var id = connection.RemoteEndPoint.SteamId.Id;
            var name = SteamFriends.GetFriendPersonaName(new CSteamID(id));
            var player = Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.source.RemoteEndPoint.SteamId.Id == id);
            if (player == null) return;

            Debug.Log($"{id}/{name} quit");

            // Call hook for plugins
            Interface.Call("OnPlayerDisconnected", player);

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerDisconnect(player);
            Interface.Call("OnUserDisconnected", covalence.PlayerManager.GetPlayer(id.ToString()), "Unknown");
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="evt"></param>
        [HookMethod("OnPlayerChat")]
        private object OnPlayerChat(ChatEvent evt)
        {
            var player = Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.networkId == evt.Sender);
            if (player == null) return null;

            var id = player.source.RemoteEndPoint.SteamId.Id;
            var name = SteamFriends.GetFriendPersonaName(new CSteamID(id));

            Debug.Log($"[Chat] {name}: {evt.Message}");

            // Call covalence hook
            return Interface.Call("OnUserChat", covalence.PlayerManager.GetPlayer(id.ToString()), evt.Message);
        }

        /// <summary>
        /// Called when the player spawns
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(BoltEntity player)
        {
            // Call covalence hook
            Interface.Call("OnUserSpawn", covalence.PlayerManager.GetPlayer(player.source.RemoteEndPoint.SteamId.Id.ToString()));
        }

        #endregion

        #region Server Magic

        /// <summary>
        /// Disables the audio output
        /// </summary>
        public static void DisableAudio()
        {
            MainMenuAudio.FadeOut();
            AudioListener.pause = true;
            AudioListener.volume = 0f;
            NGUITools.soundVolume = 0f;
            PlayerPreferences.Volume = 0f;
            PlayerPreferences.MusicVolume = 0f;

            var audioListeners = UnityEngine.Object.FindObjectsOfType<AudioListener>();
            foreach (var audioListener in audioListeners) audioListener.enabled = false;
            var audioSources = UnityEngine.Object.FindObjectsOfType<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.mute = true;
                audioSource.volume = 0f;
                audioSource.enabled = false;
            }
            var carAudios = UnityEngine.Object.FindObjectsOfType<CarAudio>();
            foreach (var carAudio in carAudios)
            {
                var components = carAudio.GetComponents<AudioSource>();
                foreach (var audioSource in components) UnityEngine.Object.Destroy(audioSource);
                carAudio.enabled = false;
            }
            var eventEmitters = Resources.FindObjectsOfTypeAll<FMOD_StudioEventEmitter>();
            foreach (var eventEmitter in eventEmitters)
            {
                eventEmitter.playOnceOnly = true;
                eventEmitter.startEventOnAwake = false;
                eventEmitter.startEventOnTriggerEnter = false;
                eventEmitter.SetVolume(0f);
                eventEmitter.Stop();
                eventEmitter.enabled = false;
            }
        }

        /// <summary>
        /// Initializes the server
        /// </summary>
        [HookMethod("InitServer")]
        private void InitServer()
        {
            VirtualCursor.Instance.enabled = false;

            Interface.Oxide.NextTick(() =>
            {
                var coop = UnityEngine.Object.FindObjectOfType<TitleScreen>();
                coop.OnCoOp();
                coop.OnMpHost();

                // Check for saved games
                if (LevelSerializer.SavedGames.Count > 0)
                {
                    coop.OnLoad();
                    coop.OnSlotSelection((int)TitleScreen.StartGameSetup.Slot);
                }
                else
                {
                    coop.OnNewGame();
                }
            });
        }

        /// <summary>
        /// Sets up the coop lobby
        /// </summary>
        [HookMethod("ILobbySetup")]
        private void ILobbySetup(Enum screen)
        {
            var type = typeof(CoopSteamNGUI).GetNestedTypes(BindingFlags.NonPublic).FirstOrDefault(x => x.IsEnum && x.Name.Equals("Screens"));
            var enumValue = type?.GetField("LobbySetup", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (enumValue != null) if (Convert.ToInt32(screen) != (int)enumValue) return;

            Interface.Oxide.NextTick(() =>
            {
                var coop = UnityEngine.Object.FindObjectOfType<CoopSteamNGUI>();
                coop.OnHostLobbySetup();
            });
        }

        /// <summary>
        /// Starts the server from lobby screen
        /// </summary>
        [HookMethod("ILobbyReady")]
        private void ILobbyReady()
        {
            Interface.Oxide.NextTick(() =>
            {
                var coop = UnityEngine.Object.FindObjectOfType<CoopSteamNGUI>();
                coop.OnHostStartGame();
            });
        }

        /// <summary>
        /// Overrides the default save path
        /// </summary>
        /// <returns></returns>
        [HookMethod("IGetSavePath")]
        private string IGetSavePath()
        {
            var saveDir = Path.Combine(Interface.Oxide.RootDirectory, "saves/");
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
            return saveDir;
        }

        /// <summary>
        /// Disables client-side elements
        /// </summary>
        private static void DisableClient()
        {
            //var gameObject = GameObject.Find("PlayerPlanePosition");
            //if (gameObject) LocalPlayer.CamFollowHead.planePos = gameObject.transform; // Causes NRE

            /*if (Scene.SceneTracker.allPlayers.Contains(LocalPlayer.Entity.gameObject)) // Useless?
                Scene.SceneTracker.allPlayers.Remove(LocalPlayer.Entity.gameObject);
            if (Scene.SceneTracker.allPlayerEntities.Contains(LocalPlayer.Entity)) // Useless?
                Scene.SceneTracker.allPlayerEntities.Remove(LocalPlayer.Entity);*/

            //LocalPlayer.Stats.KillMeFast(); // Useless?

            //var firstPersonCharacters = Resources.FindObjectsOfTypeAll<FirstPersonCharacter>();
            //foreach (var firstPersonCharacter in firstPersonCharacters) UnityEngine.Object.Destroy(firstPersonCharacter);
            //var playerStats = Resources.FindObjectsOfTypeAll<PlayerStats>();
            //foreach (var playerStat in playerStats) UnityEngine.Object.Destroy(playerStat);
            //var playerTuts = Resources.FindObjectsOfTypeAll<PlayerTuts>();
            //foreach (var playerTut in playerTuts) UnityEngine.Object.Destroy(playerTut);
            //var seasonGreebleLayers = Resources.FindObjectsOfTypeAll<SeasonGreebleLayers>();
            //foreach (var seasonGreebleLayer in seasonGreebleLayers) UnityEngine.Object.Destroy(seasonGreebleLayer);*/

            //var amplifyMotionCameras = Resources.FindObjectsOfTypeAll<AmplifyMotionCamera>();
            //foreach (var amplifyMotionCamera in amplifyMotionCameras) UnityEngine.Object.Destroy(amplifyMotionCamera);
            //var amplifyMotionEffectBases = Resources.FindObjectsOfTypeAll<AmplifyMotionEffectBase>();
            //foreach (var amplifyMotionEffectBase in amplifyMotionEffectBases) UnityEngine.Object.Destroy(amplifyMotionEffectBase);
            //var imageEffectBases = Resources.FindObjectsOfTypeAll<ImageEffectBase>();
            //foreach (var imageEffectBase in imageEffectBases) UnityEngine.Object.Destroy(imageEffectBase); // Causes PlayerStats.CheckStats NRE
            //var imageEffectOptimizers = Resources.FindObjectsOfTypeAll<ImageEffectOptimizer>();
            //foreach (var imageEffectOptimizer in imageEffectOptimizers) UnityEngine.Object.Destroy(imageEffectOptimizer);
            //var postEffectsBases = Resources.FindObjectsOfTypeAll<PostEffectsBase>();
            //foreach (var postEffectsBase in postEffectsBases) UnityEngine.Object.Destroy(postEffectsBase);
            //var scionPostProcesses = Resources.FindObjectsOfTypeAll<ScionPostProcess>();
            //foreach (var scionPostProcess in scionPostProcesses) UnityEngine.Object.Destroy(scionPostProcess);
            var projectedGrids = Resources.FindObjectsOfTypeAll<ProjectedGrid>();
            foreach (var projectedGrid in projectedGrids) UnityEngine.Object.Destroy(projectedGrid);
            var waveSpectra = Resources.FindObjectsOfTypeAll<WaveSpectrum>();
            foreach (var waveSpectrum in waveSpectra) UnityEngine.Object.Destroy(waveSpectrum);
            var planarReflections = Resources.FindObjectsOfTypeAll<PlanarReflection>();
            foreach (var planarReflection in planarReflections) UnityEngine.Object.Destroy(planarReflection);
            var underWaters = Resources.FindObjectsOfTypeAll<UnderWater>();
            foreach (var underWater in underWaters) UnityEngine.Object.Destroy(underWater);
            var oceans = Resources.FindObjectsOfTypeAll<Ocean>();
            foreach (var ocean in oceans) UnityEngine.Object.Destroy(ocean);

            var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (!behaviour.GetType().FullName.StartsWith("UI")) continue;
                //behaviour.enabled = false;
                //behaviour.gameObject.SetActive(false); // Causes save "Starting client..." issues
            }
        }

        #endregion
    }
}
