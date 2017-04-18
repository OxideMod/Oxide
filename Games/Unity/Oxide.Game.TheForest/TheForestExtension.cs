using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Extensions;
using UnityEngine;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class TheForestExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "TheForest";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

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
            "[Steamworks.NET] SteamAPI_Init() failed.",
            "GameServer.InitSafe success:",
            "Set a LogOnAnonymous",
            "Started.",
            "SteamManager - Awake",
            "SteamManager - Someone call OnDestroy",
            "SteamManager OnEnable"
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
        public static CommandLine CommandLine;

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            // Get the game's version from mainData
            var regex = new Regex(@"Version v(\d+\.\d+[a-z]?)"); // TODO: Make global static
            using (var reader = new StreamReader(Path.Combine(Application.dataPath, "level0")))
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
            Application.logMessageReceived += HandleLog;

            if (Interface.Oxide.EnableConsole()) Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{BoltNetwork.connections.Count()} | {SteamDSConfig.ServerName ?? "Unnamed"}";

            Interface.Oxide.ServerConsole.Status1Left = () => SteamDSConfig.ServerName ?? "Unnamed";
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{BoltNetwork.connections.Count()}/{SteamDSConfig.ServerPlayers}";
            /*Interface.Oxide.ServerConsole.Status2Right = () =>
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
            };*/

            //Interface.Oxide.ServerConsole.Status3Left = () => DateTime.Today.AddMinutes(TheForestAtmosphere.Instance.TimeOfDay).ToString("h:mm tt").ToLower();
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {SteamDSConfig.ServerVersion}";
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
            logWriter.WriteLine(message); // TODO: Fix access violation
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
