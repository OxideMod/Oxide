using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class TheForestExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "TheForest";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "Bolt", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "TheForest", "UnityEngine"
        };

        public static string[] Filter =
        {
            "****** Game Activation Sequence ******",
            "65K cleanup",
            "<color=red>Ceto",
            "<color=yellow>Ceto",
            "Body Variation",
            "Broken ItemCondition, likely serializer didn't load it correctly",
            "CanResume:",
            "Can't remove Rigidbody because",
            "Cancel/done player respawn",
            "Collapse:",
            "CoopLobby.LeaveActive instance=",
            "CoopSteamServer.P2PSessionRequest",
            "DestroyPickup:",
            "Displacement Rendertex recreated",
            "FMOD Error (ERR_INVALID_PARAM)",
            "Frost Damage",
            "Game Activation Sequence step",
            "HDR RenderTexture format is not supported on this platform",
            "HealedMp",
            "HitPlayer:",
            "Hull (UnityEngine.GameObject)",
            "Image Effects are not supported on this platform",
            "InitMaterial Starfield",
            "Joystick count=",
            "LobbyCreated param.m_eResult=k_EResult",
            "LocalPlayer -> Respawn",
            "No LOD Manager Found, please add one",
            "No Work Scheduler found, please add one",
            "OnApplicationFocus:",
            "OnPlaced",
            "PlayerPreferences.Load",
            "Refreshing Input Mapping Icons",
            "Reloading Input Mapping",
            "RewiredSpawner",
            "Saving",
            "Setting fake plane coordinates for testing purposes",
            "Skin Variation",
            "Skipped frame because",
            "Skipped rendering frame because",
            "SpawnPool creatures:",
            "Starvation Damage",
            "The referenced script on this Behaviour is missing!",
            "Thirst Damage",
            "WakeFromKnockOut",
            "Wrong bolt state on:",
            "[AmplifyMotion] Initialization failed",
            "all clients exited cave",
            "attach:",
            "attached:",
            "can't use image filters",
            "client entered cave",
            "delaying initial",
            "disableFlying",
            "disablePlaneCrash",
            "doing player in plane",
            "doing prediction on player from enemy",
            "enabled part",
            "fake plane loaded",
            "going black",
            "killed player",
            "null texture passed to GUI.DrawTexture",
            "planeCrash started",
            "playerControl enabled at",
            "set trap for dummy mutant",
            "setFemale",
            "setMale",
            "setPlanePosition site=",
            "setting clothes",
            "setting group encounter for",
            "smooth unlock",
            "spawner was destroyed",
            "started steam server"
        };

        /// <summary>
        /// Initializes a new instance of the TheForestExtension class
        /// </summary>
        /// <param name="manager"></param>
        public TheForestExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new TheForestPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        public static string GameVersion = "Unknown";
        private const string logFileName = "logs/output_log.txt"; // TODO: Add -logfile support
        private TextWriter logWriter;

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
            /*var carAudios = UnityEngine.Object.FindObjectsOfType<CarAudio>();
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
            }*/
        }

        /// <summary>
        /// Disables client-side elements
        /// </summary>
        private static void DisableClient()
        {
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
            //var projectedGrids = Resources.FindObjectsOfTypeAll<ProjectedGrid>();
            //foreach (var projectedGrid in projectedGrids) UnityEngine.Object.Destroy(projectedGrid);
            //var waveSpectra = Resources.FindObjectsOfTypeAll<WaveSpectrum>();
            //foreach (var waveSpectrum in waveSpectra) UnityEngine.Object.Destroy(waveSpectrum);
            //var planarReflections = Resources.FindObjectsOfTypeAll<PlanarReflection>();
            //foreach (var planarReflection in planarReflections) UnityEngine.Object.Destroy(planarReflection);
            //var underWaters = Resources.FindObjectsOfTypeAll<UnderWater>();
            //foreach (var underWater in underWaters) UnityEngine.Object.Destroy(underWater);
            //var oceans = Resources.FindObjectsOfTypeAll<Ocean>();
            //foreach (var ocean in oceans) UnityEngine.Object.Destroy(ocean);

            /*var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (!behaviour.GetType().FullName.StartsWith("UI")) continue;
                behaviour.enabled = false;
                behaviour.gameObject.SetActive(false);
            }*/
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            // Get the game's version from mainData
            var regex = new Regex(@"Version v(\d+\.\d+[a-z]?)");
            using (var reader = new StreamReader(Path.Combine(Application.dataPath, "mainData")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;
                    GameVersion = match.Groups[1].Value;
                }
            }

            if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
            if (File.Exists(logFileName)) File.Delete(logFileName);
            var logStream = File.AppendText(logFileName);
            logStream.AutoFlush = true;
            logWriter = TextWriter.Synchronized(logStream);
            Application.logMessageReceivedThreaded += HandleLog;

            // Limit FPS to reduce CPU usage
            PlayerPreferences.MaxFrameRate = 60;

            // Override default server settings
            var commandLine = new CommandLine(Environment.GetCommandLineArgs());
            if (commandLine.HasVariable("maxplayers")) PlayerPrefs.SetInt("MpGamePlayerCount", int.Parse(commandLine.GetVariable("maxplayers")));
            if (commandLine.HasVariable("hostname")) PlayerPrefs.SetString("MpGameName", commandLine.GetVariable("hostname"));
            if (commandLine.HasVariable("friendsonly")) PlayerPrefs.SetInt("MpGameFriendsOnly", int.Parse(commandLine.GetVariable("friendsonly")));
            if (commandLine.HasVariable("saveslot")) PlayerPrefs.SetInt("MpGameSaveSlot", int.Parse(commandLine.GetVariable("saveslot")));
            //if (commandLine.HasVariable("saveinterval")) // TODO: Make this work
            //if (commandLine.HasVariable("gamemode")) // TODO: Make this work

            // Check if client should be disabled
            if (commandLine.HasVariable("batchmode") || commandLine.HasVariable("nographics"))
            {
                // Disable audio and client-side elements if not dedicated
                DisableAudio();
                DisableClient();
            }

            if (Interface.Oxide.EnableConsole()) Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{CoopLobby.Instance?.MemberCount ?? 0} | {CoopLobby.Instance?.Info?.Name ?? "Unnamed"}";

            Interface.Oxide.ServerConsole.Status1Left = () => CoopLobby.Instance?.Info.Name ?? "Unnamed";
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{CoopLobby.Instance?.MemberCount}/{CoopLobby.Instance?.Info.MemberLimit}";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (BoltNetwork.serverTime <= 0) return "not connected";

                double bytesReceived = 0;
                double bytesSent = 0;
                foreach (var connection in BoltNetwork.connections)
                {
                    bytesReceived += connection.BitsPerSecondOut / 8f;
                    bytesSent += connection.BitsPerSecondIn / 8f;
                }
                return $"{Utility.FormatBytes(bytesReceived)}/s in, {Utility.FormatBytes(bytesSent)}/s out";
            };

            Interface.Oxide.ServerConsole.Status3Left = () => DateTime.Today.AddMinutes(TheForestAtmosphere.Instance.TimeOfDay).ToString("h:mm tt").ToLower();
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {GameVersion}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        public override void OnShutdown() => logWriter?.Close();

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Handle console input
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;
            logWriter.WriteLine(message);
            if (!string.IsNullOrEmpty(stackTrace)) logWriter.WriteLine(stackTrace);

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
