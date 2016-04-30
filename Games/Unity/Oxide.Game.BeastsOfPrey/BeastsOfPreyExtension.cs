using System;
using System.Linq;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Game.BeastsOfPrey
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class BeastsOfPreyExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "BeastsOfPrey";

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
            "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
        };

        /// <summary>
        /// Initializes a new instance of the BeastsOfPreyExtension class
        /// </summary>
        /// <param name="manager"></param>
        public BeastsOfPreyExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new BeastsOfPreyPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("BoP", new Libraries.BeastsOfPrey());
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
            if (!Interface.Oxide.EnableConsole(true)) return;

            Application.logMessageReceived += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;

            Interface.Oxide.ServerConsole.Title = () =>
            {
                var players = ServerMonitor.instance?.playerList.childCount;
                var hostname = ServerMonitor.instance?.serverName;
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                var hostname = ServerMonitor.instance?.serverName;
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
                var players = ServerMonitor.instance?.playerList.childCount;
                const int playerLimit = 20; // SmartFoxServer license limitation
                return string.Concat(" ", players, "/", playerLimit, " players");
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                // TODO: Network in/out
                return string.Empty;
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = DateTime.Today.Add(TimeSpan.FromSeconds(Server_Network_Manager.instance.timeServer)).ToString("h:mm tt").ToLower();
                return string.Concat(" ", time);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = Server_Network_Manager.instance.sfsVersion;
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
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
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
