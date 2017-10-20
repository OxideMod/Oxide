﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Bolt;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using UnityEngine;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class TheForestExtension : Extension
    {
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static AssemblyName AssemblyName = Assembly.GetName();
        internal static VersionNumber AssemblyVersion = new VersionNumber(AssemblyName.Version.Major, AssemblyName.Version.Minor, AssemblyName.Version.Build);
        internal static string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly, typeof(AssemblyCompanyAttribute), false)).Company;

        /// <summary>
        /// Gets whether this extension is for a specific game
        /// </summary>
        public override bool IsGameExtension => true;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "TheForest";

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => AssemblyAuthors;

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => AssemblyVersion;

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
            "------- Rewired System Info -------",
            "CoopLobby.LeaveActive instance",
            "CoopSteamManager Initialize",
            "Game Activation Sequence step",
            "GameServer.InitSafe success:",
            "InitMaterial Starfield",
            "PlayerPreferences.Load",
            "Please initialize AssetBundleManifest by calling",
            "Refreshing Input Mapping Icons",
            "Set a LogOnAnonymous",
            "Started.",
            "SteamManager - Awake",
            "SteamManager - Someone call OnDestroy",
            "SteamManager OnEnable",
            "Trying to reload asset from disk that is not stored on disk",
            "initializing asset bundle manager with manifest:",
            "setPlanePosition site=",
            "setting asset bundle manifest",
            "starting enemy spawn",
            "starting to load manifest"
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
            Manager.RegisterPluginLoader(new TheForestPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        private const string logFileName = "logs/output_log.txt"; // TODO: Add -logfile support
        private TextWriter logWriter;

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
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

            Interface.Oxide.ServerConsole.Status2Left = () => $"{BoltNetwork.clients.Count()}/{SteamDSConfig.ServerPlayers} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (Time.realtimeSinceStartup < 0 /*!SteamDSConfig.connectedToSteam*/) return "not connected";

                double bytesReceived = 0;
                double bytesSent = 0;
                foreach (var connection in BoltNetwork.clients)
                {
                    bytesReceived += connection.BitsPerSecondOut / 8f;
                    bytesSent += connection.BitsPerSecondIn / 8f;
                }
                return $"{Utility.FormatBytes(bytesReceived)}/s in, {Utility.FormatBytes(bytesSent)}/s out";
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                //var gameTime = Scene.Atmosphere.TimeOfDay/*).ToString("h:mm tt")*/;
                return $"Slot {SteamDSConfig.GameSaveSlot} [{SteamDSConfig.GameDifficulty}]";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {SteamDSConfig.ServerVersion}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        public override void OnShutdown() => logWriter?.Close();

        private static void ServerConsoleOnInput(string input)
        {
            input = input.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                var inputArray = input.Split();
                var adminCommand = AdminCommand.Create(GlobalTargets.OnlyServer);
                adminCommand.Command = inputArray[0];
                adminCommand.Data = string.Concat(inputArray.Skip(1).Select(s => s).ToArray());
                adminCommand.Send();
            }
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

            logWriter.WriteLine(message); // TODO: Fix access violation
            if (!string.IsNullOrEmpty(stackTrace)) logWriter.WriteLine(stackTrace);

            var color = ConsoleColor.Gray;
            var remoteType = "generic";
            if (type == LogType.Warning)
            {
                color = ConsoleColor.Yellow;
                remoteType = "warning";
            }
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                color = ConsoleColor.Red;
                remoteType = "error";
            }
            Interface.Oxide.ServerConsole.AddMessage(message, color);
            Interface.Oxide.RemoteConsole.SendMessage(new RemoteMessage
            {
                Message = message,
                Identifier = 0,
                Type = remoteType,
                Stacktrace = stackTrace
            });
        }
    }
}
