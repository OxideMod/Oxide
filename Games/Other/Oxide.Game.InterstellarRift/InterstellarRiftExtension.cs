using System;
using System.Linq;
using System.Reflection;
using Game.Configuration;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;

namespace Oxide.Game.InterstellarRift
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class InterstellarRiftExtension : Extension
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
        public override string Name => "InterstellarRift";

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

            Interface.Oxide.ServerConsole.Title = () => $"? | {ServerConfig.Singleton.ServerName}";

            Interface.Oxide.ServerConsole.Status1Left = () => ServerConfig.Singleton.ServerName;
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

        private static void HandleLog(string message, string stackTrace)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

            var color = ConsoleColor.Gray;
            var remoteType = "generic";

            // TODO: Color handling

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
