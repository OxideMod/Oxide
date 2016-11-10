using System;
using Game.Configuration;
using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Game.InterstellarRift
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class InterstellarRiftExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "InterstellarRift";

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
            "mscorlib", "Oxide.Core", "System", "System.Core"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "System.Collections", "System.Security.Cryptography", "System.Text"
        };

        public static string[] Filter =
        {
        };

        /// <summary>
        /// Initializes a new instance of the InterstellarRiftExtension class
        /// </summary>
        /// <param name="manager"></param>
        public InterstellarRiftExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new InterstellarRiftPluginLoader());
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
            if (!Interface.Oxide.EnableConsole()) return;

            // TODO: Add console log handling

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"? | {Config.Singleton.ServerName}";

            Interface.Oxide.ServerConsole.Status1Left = () => Config.Singleton.ServerName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = string.Empty; // TODO: Implement when possible
                var time = TimeSpan.FromSeconds(0); // TODO: Implement when possible
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };
            /*Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = string.Empty; // TODO: Implement when possible
                var playerLimit = string.Empty; // TODO: Implement when possible
                return string.Concat(" ", players, "/", playerLimit, " players");
            };*/
            /*Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                var bytesReceived = Utility.FormatBytes(0); // TODO: Implement when possible
                var bytesSent = Utility.FormatBytes(0); // TODO: Implement when possible
                return null <= 0 ? "0b/s in, 0b/s out" : string.Concat(bytesReceived, "/s in, ", bytesSent, "/s out"); // TODO: Implement when possible
            };*/

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = DateTime.Today.AddSeconds(0).ToString("h:mm tt").ToLower(); // TODO: Implement when possible
                return string.Concat(" ", time); // TODO: Add more info
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {Globals.Version}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Handle console input
        }
    }
}
