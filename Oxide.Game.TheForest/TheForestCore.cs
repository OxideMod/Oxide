using System;
using System.Linq;
using System.Reflection;

using ScionEngine;
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

            var plugins = Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins");
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
            RemoteLogger.SetTag("protocol", "0.24b"); // TODO: Grab version/protocol
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
        private void OnServerShutdown()
        {
            Interface.Oxide.OnShutdown();
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore")
                InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }

        public static void DisableAudio()
        {
            PlayerPrefs.SetFloat("Sound", 0f);
            PlayerPrefs.SetFloat("Volume", 0f); // This doesn't seem to work for some/all
            PlayerPrefs.SetFloat("MusicVolume", 0f); // This doesn't seem to work some/all
            MainMenuAudio.FadeOut();
            AudioListener.pause = true;
            AudioListener.volume = 0f;
            NGUITools.soundVolume = 0f;
            PlayerPreferences.Volume = 0f;
            PlayerPreferences.MusicVolume = 0f;
            if (Scene.PlaneCrash != null) Scene.PlaneCrash.ShowCrash = false;

            var audioListeners = UnityEngine.Object.FindObjectsOfType<AudioListener>();
            foreach (var audioListener in audioListeners)
            {
                Interface.Oxide.LogInfo("Disable AudioListener: {0} {1}", audioListener.name, audioListener.GetInstanceID());
                audioListener.enabled = false;
                //audioListener.gameObject.SetActive(false);
            }
            var audioSources = UnityEngine.Object.FindObjectsOfType<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                Interface.Oxide.LogInfo("Disable AudioSource: {0} {1}", audioSource.name, audioSource.GetInstanceID());
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.mute = true;
                audioSource.volume = 0f;
                audioSource.enabled = false;
                //audioSource.gameObject.SetActive(false);
            }
            var carAudios = UnityEngine.Object.FindObjectsOfType<CarAudio>();
            foreach (var carAudio in carAudios)
            {
                Interface.Oxide.LogInfo("Disable CarAudio: {0} {1}", carAudio.name, carAudio.GetInstanceID());
                var components = carAudio.GetComponents<AudioSource>();
                foreach (var audioSource in components)
                {
                    UnityEngine.Object.Destroy(audioSource);
                }
                carAudio.enabled = false;
            }
            var eventEmitters = UnityEngine.Resources.FindObjectsOfTypeAll<FMOD_StudioEventEmitter>();
            foreach (var eventEmitter in eventEmitters)
            {
                Interface.Oxide.LogInfo("Disable FMOD_StudioEventEmitter: {0} {1}", eventEmitter.name, eventEmitter.GetInstanceID());
                eventEmitter.playOnceOnly = true;
                eventEmitter.startEventOnAwake = false;
                eventEmitter.startEventOnTriggerEnter = false;
                eventEmitter.SetVolume(0f);
                eventEmitter.Stop();
                eventEmitter.enabled = false;
            }
        }

        [HookMethod("IOnBeginPlaneCrash")]
        private bool IOnBeginPlaneCrash(TriggerCutScene triggerCutScene)
        {
            Interface.Oxide.LogInfo("Disable TriggerCutScene: {0} {1}", triggerCutScene.name, triggerCutScene.GetInstanceID());
            var skipOpeningAnimationMethod = typeof (TriggerCutScene).GetMethod("skipOpeningAnimation", BindingFlags.NonPublic | BindingFlags.Instance);
            var CleanUpMethod = typeof (TriggerCutScene).GetMethod("CleanUp", BindingFlags.NonPublic | BindingFlags.Instance);
            try
            {
                var gameObject = GameObject.Find("PlayerPlanePosition");
                if (gameObject)
                {
                    LocalPlayer.CamFollowHead.planePos = gameObject.transform;
                }
                skipOpeningAnimationMethod.Invoke(triggerCutScene, null);
                CleanUpMethod.Invoke(triggerCutScene, null);
            }
            catch (Exception e)
            {
                Interface.Oxide.LogException("OnTriggerCutSceneAwake: ", e);
            }
            triggerCutScene.CancelInvoke("beginPlaneCrash");
            triggerCutScene.planeController.CancelInvoke("beginPlaneCrash");
            triggerCutScene.planeController.enabled = false;
            triggerCutScene.planeController.gameObject.SetActive(false);
            triggerCutScene.enabled = false;
            triggerCutScene.gameObject.SetActive(false);
            var amplifyMotionEffectBases = UnityEngine.Resources.FindObjectsOfTypeAll<AmplifyMotionEffectBase>();
            foreach (var amplifyMotionEffectBase in amplifyMotionEffectBases)
            {
                Interface.Oxide.LogInfo("Disable AmplifyMotionEffectBase: {0} {1}", amplifyMotionEffectBase.name, amplifyMotionEffectBase.GetInstanceID());
                UnityEngine.Object.Destroy(amplifyMotionEffectBase);
            }
            var imageEffectOptimizers = UnityEngine.Resources.FindObjectsOfTypeAll<ImageEffectOptimizer>();
            foreach (var imageEffectOptimizer in imageEffectOptimizers)
            {
                Interface.Oxide.LogInfo("Disable ImageEffectOptimizer: {0} {1}", imageEffectOptimizer.name, imageEffectOptimizer.GetInstanceID());
                UnityEngine.Object.Destroy(imageEffectOptimizer);
            }
            var scionPostProcesses = UnityEngine.Resources.FindObjectsOfTypeAll<ScionPostProcess>();
            foreach (var scionPostProcess in scionPostProcesses)
            {
                Interface.Oxide.LogInfo("Disable ScionPostProcess: {0} {1}", scionPostProcess.name, scionPostProcess.GetInstanceID());
                UnityEngine.Object.Destroy(scionPostProcess);
            }
            var behaviours = UnityEngine.Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (!behaviour.GetType().FullName.StartsWith("UI")) continue;
                Interface.Oxide.LogInfo("Disable {0}: {1} {2}", behaviour.GetType(), behaviour.name, behaviour.GetInstanceID());
                behaviour.enabled = false;
                behaviour.gameObject.SetActive(false);
                //UnityEngine.Object.Destroy(behaviour);
            }
            DisableAudio();
            return false;
        }

        /// <summary>
        /// Initializes the server
        /// </summary>
        [HookMethod("InitServer")]
        private void InitServer()
        {
            Interface.Oxide.LogInfo("InitServer");
            VirtualCursor.Instance.enabled = false;
            Interface.Oxide.NextTick(() =>
            {
                Interface.Oxide.LogInfo("InitServer NextTick");

                var coop = UnityEngine.Object.FindObjectOfType<TitleScreen>();
                coop.OnCoOp();
                coop.OnMpHost();
                coop.OnNewGame();
                DisableAudio();
            });
        }

        /// <summary>
        /// Initializes the server lobby
        /// </summary>
        [HookMethod("InitLobby")]
        private void InitLobby(CoopSteamNGUI ngui, Enum screen)
        {
            Interface.Oxide.LogInfo("InitLobby");

            Type type = typeof(CoopSteamNGUI).GetNestedTypes(BindingFlags.NonPublic).FirstOrDefault(x => x.IsEnum && x.Name.Equals("Screens"));
            Interface.Oxide.LogInfo("Type: {0}", type);
            object enumValue = type.GetField("LobbySetup", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            Interface.Oxide.LogInfo("Screen: {0} Value: {0} Name: {1}", Convert.ToInt32(screen), (int)enumValue, Enum.GetName(type, enumValue));
            if (Convert.ToInt32(screen) != (int)enumValue) return;
            Interface.Oxide.NextTick(() =>
            {
                Interface.Oxide.LogInfo("InitLobby NextTick");

                var coop = UnityEngine.Object.FindObjectOfType<CoopSteamNGUI>();
                coop.OnHostLobbySetup();
                DisableAudio();
            });
        }
    }
}
