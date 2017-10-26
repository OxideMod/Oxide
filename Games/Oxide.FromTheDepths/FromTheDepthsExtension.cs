using BrilliantSkies.FromTheDepths.Game.UserInterfaces;
using BrilliantSkies.FromTheDepths.Multiplayer;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Oxide.Game.FromTheDepths
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class FromTheDepthsExtension : Extension
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
        public override string Name => "FromTheDepths";

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => AssemblyAuthors;

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => AssemblyVersion;

        /// <summary>
        /// Default game-specific references for use in plugins
        /// </summary>
        internal static readonly HashSet<string> DefaultReferences = new HashSet<string>
        {
        };

        /// <summary>
        /// List of assemblies allowed for use in plugins
        /// </summary>
        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine"
        };

        public override string[] WhitelistNamespaces => new[]
        {
            "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
        };

        /// <summary>
        /// Initializes a new instance of the FromTheDepthsExtension class
        /// </summary>
        /// <param name="manager"></param>
        public FromTheDepthsExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new FromTheDepthsPluginLoader());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            CSharpPluginLoader.PluginReferences.UnionWith(DefaultReferences);

            if (!Interface.Oxide.EnableConsole()) return;

            // Disable splash screens
            //Application.LoadLevel("Main menu"); // TODO: Hit it with a hammer

            // Disable client audio
            PlayerProfileOptionsSound.i.MusicVolume = 0f;
            PlayerProfileOptionsSound.i.SoundVolume = 0f;

            // Set server options
            PlayerProfileOptionsMultiplayer.i.GameName = "My Oxide Server"; // TODO: Switch to cmd argument
            PlayerProfileOptionsMultiplayer.i.GameComments = "Powered by Oxide"; // TODO: Switch to cmd argument
            PlayerProfileOptionsMultiplayer.i.GamePassword = "oxide"; // TODO: Switch to cmd argument
            PlayerProfileOptionsMultiplayer.i.PlayerLimit = 10; // TODO: Switch to cmd argument

            // Start server
            HostMenuGUI.Instance.HostGame();
            //var selectedMapInstance = Planet.i.MultiplayerMaps.Instances[1]; // TODO: ?
            HostMenuGUI.Instance.LaunchServer();
            GameLobbyGUI.Instance.StartGame();

            Application.logMessageReceived += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () =>
            {
                if (MultiplayerManager.Instance == null) return string.Empty;
                return $"{MultiplayerManager.Instance.Players.PlayerCount} | {MultiplayerManager.Instance.GameDetails.Name}";
            };

            Interface.Oxide.ServerConsole.Status1Left = () => $"{MultiplayerManager.Instance?.GameDetails.Name ?? "Unnamed"}";
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                if (MultiplayerManager.Instance == null) return string.Empty;
                return $"{MultiplayerManager.Instance.Players.PlayerCount}/{MultiplayerManager.Instance.GameDetails.PlayerLimit}";
            };
            Interface.Oxide.ServerConsole.Status2Right = () => string.Empty; // TODO: Network in/out

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                if (GameTimer.Instance == null) return string.Empty;
                var time = DateTime.Today.AddSeconds(GameTimer.Instance.GameTime).ToString("h:mm tt").ToLower();
                var map = string.Empty; // TODO: MPGameData.MapName
                return string.Concat(time, ", ", map);
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {StaticOptionsManager.version}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Handle console input
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

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
