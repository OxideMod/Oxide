using System;
using System.IO;
using System.Linq;

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

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine" };
        public override string[] WhitelistNamespaces => new[] { "Bolt", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "TheForest", "UnityEngine" };

        public static string[] Filter =
        {
            "****** Game Activation Sequence ******",
            "65K cleanup",
            "<color=red>Ceto",
            "<color=yellow>Ceto",
            "Body Variation",
            "CanResume:",
            "Cancel/done player respawn",
            "Collapse:",
            "CoopLobby.LeaveActive instance=",
            "CoopSteamServer.P2PSessionRequest",
            "DestroyPickup:",
            "Displacement Rendertex recreated",
            "Frost Damage",
            "Game Activation Sequence step",
            "HDR RenderTexture format is not supported on this platform",
            "HealedMp",
            "HitPlayer:",
            "Hull (UnityEngine.GameObject)",
            "Image Effects are not supported on this platform",
            "Joystick count=",
            "LobbyCreated param.m_eResult=k_EResult",
            "LocalPlayer -> Respawn",
            "OnApplicationFocus:",
            "OnPlaced",
            "PlayerPreferences.Load",
            "Refreshing Input Mapping Icons",
            "Reloading Input Mapping",
            "RewiredSpawner",
            "Saving",
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
            "enabled part",
            "going black",
            "null texture passed to GUI.DrawTexture",
            "planeCrash started",
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

        private const string LogFileName = "output_log.txt"; // TODO: Add -logFile support
        private TextWriter logWriter;

        /// <summary>
        /// Initializes a new instance of the TheForestExtension class
        /// </summary>
        /// <param name="manager"></param>
        public TheForestExtension(ExtensionManager manager)
            : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new TheForestPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Forest", new Libraries.TheForest());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public override void LoadPluginWatchers(string pluginDirectory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            if (!Interface.Oxide.EnableConsole()) return;

            if (File.Exists(LogFileName)) File.Delete(LogFileName);
            var logStream = File.AppendText(LogFileName);
            logStream.AutoFlush = true;
            logWriter = TextWriter.Synchronized(logStream);

            Application.logMessageReceivedThreaded += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;

            // Override default server settings
            //var boltInit = UnityEngine.Object.FindObjectOfType<BoltInit>();
            //var serverAddress = typeof(BoltInit).GetField("serverAddress", BindingFlags.NonPublic | BindingFlags.Instance);
            //var serverPort = typeof(BoltInit).GetField("serverPort", BindingFlags.NonPublic | BindingFlags.Instance);
            var commandLine = new CommandLine(Environment.GetCommandLineArgs());
            //if (commandLine.HasVariable("ip")) serverAddress?.SetValue(boltInit, commandLine.GetVariable("ip"));
            //if (commandLine.HasVariable("port")) serverPort?.SetValue(boltInit, commandLine.GetVariable("port"));
            if (commandLine.HasVariable("maxplayers")) PlayerPrefs.SetInt("MpGamePlayerCount", int.Parse(commandLine.GetVariable("maxplayers")));
            if (commandLine.HasVariable("hostname")) PlayerPrefs.SetString("MpGameName", commandLine.GetVariable("hostname"));
            if (commandLine.HasVariable("friendsonly"))  PlayerPrefs.SetInt("MpGameFriendsOnly", int.Parse(commandLine.GetVariable("friendsonly")));
            if (commandLine.HasVariable("saveslot")) PlayerPrefs.SetInt("MpGameSaveSlot", int.Parse(commandLine.GetVariable("saveslot")));
            //if (commandLine.HasVariable("saveinterval")) /* TODO */

            // Disable client audio for server
            TheForestCore.DisableAudio();

            // Limit FPS to reduce CPU usage
            PlayerPreferences.MaxFrameRate = 60;

            Interface.Oxide.ServerConsole.Title = () =>
            {
                if (CoopLobby.Instance == null) return string.Empty;
                var players = CoopLobby.Instance.MemberCount;
                var hostname = CoopLobby.Instance.Info.Name;
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                if (CoopLobby.Instance == null) return string.Empty;
                var hostname = CoopLobby.Instance.Info.Name;
                return string.Concat(" ", hostname);
            };
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                if (CoopLobby.Instance == null) return string.Empty;
                var players = CoopLobby.Instance.MemberCount;
                var playerLimit = CoopLobby.Instance.Info.MemberLimit;
                return string.Concat(" ", players, "/", playerLimit, " players");
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                // TODO: Network in/out
                return string.Empty;
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                //var gameTime = TheForestAtmosphere.Instance?.TimeOfDay; // TODO: Fix NRE and format
                return string.Concat(string.Empty/*, gameTime*/);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = "0.33b"; // TODO: Grab version
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
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
            else if (type == LogType.Error)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
