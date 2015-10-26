using System;
using System.Linq;
using System.Reflection;

using Ceto;
using ScionEngine;
using Steamworks;
using TheForest.UI;
using TheForest.Utils;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The core The Forest plugin
    /// </summary>
    public class TheForestCore : CSPlugin
    {
        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the TheForestCore class
        /// </summary>
        public TheForestCore()
        {
            // Set attributes
            Name = "theforestcore";
            Title = "The Forest Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>("Plugins");
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "the forest");
            RemoteLogger.SetTag("protocol", "0.26"); // TODO: Grab version/protocol
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", CoopLobby.Instance?.Info.Name.Split("()".ToCharArray())[0]);
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

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

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="connection"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(BoltConnection connection)
        {
            if (connection == null) return;
            var steamId = connection.RemoteEndPoint.SteamId.Id;
            var name = SteamFriends.GetFriendPersonaName(new CSteamID(steamId));

            Interface.Oxide.LogInfo($"{steamId}/{name} joined");
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="e"></param>
        [HookMethod("OnPlayerChat")]
        private void OnPlayerChat(ChatEvent e)
        {
            var player = Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.networkId == e.Sender);
            if (player == null) return;
            var steamId = player.source.RemoteEndPoint.SteamId.Id;
            var name = SteamFriends.GetFriendPersonaName(new CSteamID(steamId));

            Interface.Oxide.LogInfo($"{name}: {e.Message}");
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="connection"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(BoltConnection connection)
        {
            if (connection == null) return;
            var steamId = connection.RemoteEndPoint.SteamId.Id;
            var name = SteamFriends.GetFriendPersonaName(new CSteamID(steamId));

            Interface.Oxide.LogInfo($"{steamId}/{name} quit");
        }

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

            if (Scene.PlaneCrash != null) Scene.PlaneCrash.ShowCrash = false;

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
        [HookMethod("IOnInitServer")]
        private void IOnInitServer()
        {
            VirtualCursor.Instance.enabled = false;
            Interface.Oxide.NextTick(() =>
            {
                var coop = UnityEngine.Object.FindObjectOfType<TitleScreen>();
                coop.OnCoOp();
                coop.OnMpHost();
                coop.OnNewGame();
                //coop.OnSlotSelection(1);
            });
        }

        /// <summary>
        /// Sets up the coop lobby
        /// </summary>
        [HookMethod("IOnCoopSetup")]
        private void IOnCoopSetup(Enum screen)
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
        [HookMethod("IOnLobbyReady")]
        private void IOnLobbyReady()
        {
            Interface.Oxide.NextTick(() =>
            {
                var coop = UnityEngine.Object.FindObjectOfType<CoopSteamNGUI>();
                coop.OnHostStartGame();
            });
        }

        /// <summary>
        /// Overrides setting of the server hostname
        /// </summary>
        [HookMethod("IOnGetHostname")]
        private string IOnGetHostname() => PlayerPrefs.GetString("MpGameName");

        /// <summary>
        /// Overrides setting of the maximum players
        /// </summary>
        [HookMethod("IOnGetMaxPlayers")]
        private int IOnGetMaxPlayers() => PlayerPrefs.GetInt("MpGamePlayerCount");

        [HookMethod("IOnPlaneCrash")]
        private bool IOnPlaneCrash(TriggerCutScene triggerCutScene)
        {
            var skipOpeningAnimationMethod = typeof(TriggerCutScene).GetMethod("skipOpeningAnimation", BindingFlags.NonPublic | BindingFlags.Instance);
            var cleanUpMethod = typeof(TriggerCutScene).GetMethod("CleanUp", BindingFlags.NonPublic | BindingFlags.Instance);
            try
            {
                var gameObject = GameObject.Find("PlayerPlanePosition");
                if (gameObject) LocalPlayer.CamFollowHead.planePos = gameObject.transform;
                skipOpeningAnimationMethod.Invoke(triggerCutScene, null);
                cleanUpMethod.Invoke(triggerCutScene, null);
            }
            catch (Exception e) { /*Interface.Oxide.LogException("OnTriggerCutSceneAwake: ", e);*/ }
            triggerCutScene.CancelInvoke("beginPlaneCrash");
            triggerCutScene.planeController.CancelInvoke("beginPlaneCrash");
            triggerCutScene.planeController.enabled = false;
            triggerCutScene.planeController.gameObject.SetActive(false);
            triggerCutScene.enabled = false;
            triggerCutScene.gameObject.SetActive(false);
            var amplifyMotionEffectBases = Resources.FindObjectsOfTypeAll<AmplifyMotionEffectBase>();
            foreach (var amplifyMotionEffectBase in amplifyMotionEffectBases) UnityEngine.Object.Destroy(amplifyMotionEffectBase);
            var imageEffectOptimizers = Resources.FindObjectsOfTypeAll<ImageEffectOptimizer>();
            foreach (var imageEffectOptimizer in imageEffectOptimizers) UnityEngine.Object.Destroy(imageEffectOptimizer);
            var scionPostProcesses = Resources.FindObjectsOfTypeAll<ScionPostProcess>();
            foreach (var scionPostProcess in scionPostProcesses) UnityEngine.Object.Destroy(scionPostProcess);
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
                behaviour.enabled = false;
                behaviour.gameObject.SetActive(false);
            }
            return false;
        }
    }
}
