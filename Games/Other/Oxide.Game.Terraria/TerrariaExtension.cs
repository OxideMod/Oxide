using System;

using Terraria;

using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Game.Terraria
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class TerrariaExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Terraria";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "mscorlib", "Oxide.Core", "System", "System.Core" };
        public override string[] WhitelistNamespaces => new[] { "System.Collections", "System.Security.Cryptography", "System.Text" };

        public static string[] Filter =
        {
        };

        /// <summary>
        /// Initializes a new instance of the TerrariaExtension class
        /// </summary>
        /// <param name="manager"></param>
        public TerrariaExtension(ExtensionManager manager)
            : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new TerrariaPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Terraria", new Libraries.Terraria());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            if (!Interface.Oxide.EnableConsole()) return;

            // TODO: Add console log handling

            Interface.Oxide.ServerConsole.Title = () =>
            {
                var players = Main.numPlayers;
                var hostname = Main.worldName;
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                var hostname = Main.worldName;
                return string.Concat(" ", hostname);
            };
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Main.fpsCount; // Main.fpsTimer
                var seconds = TimeSpan.FromSeconds(Main.time);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = Main.numPlayers; // Main.player.Count, NetPlay.Clients.Count
                var playerLimit = Main.maxNetPlayers;
                return string.Concat(" ", players, "/", playerLimit, " players");
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                var bytesReceived = Utility.FormatBytes(Main.rxData);
                var bytesSent = Utility.FormatBytes(Main.txData);
                return Main.time <= 0 ? "0b/s in, 0b/s out" : string.Concat(bytesReceived, "/s in, ", bytesSent, "/s out");
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = DateTime.Today.Add(TimeSpan.FromSeconds(Main.mapTime)).ToString("h:mm tt").ToLower();
                // TODO: More info
                return string.Concat(" ", time);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = Main.versionNumber;
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Handle console input
        }
    }
}
