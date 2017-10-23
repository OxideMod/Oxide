using System;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using Terraria;

namespace Oxide.Game.Terraria
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class TerrariaExtension : Extension
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
        public override string Name => "Terraria";

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
        /// Initializes a new instance of the TerrariaExtension class
        /// </summary>
        /// <param name="manager"></param>
        public TerrariaExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new TerrariaPluginLoader());
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

            Interface.Oxide.ServerConsole.Title = () => $"{Main.ActivePlayersCount} | {Main.worldName}";

            Interface.Oxide.ServerConsole.Status1Left = () => Main.worldName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Main.time);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Main.fpsCount}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{Main.ActivePlayersCount}/{Main.maxNetPlayers}";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                var bytesReceived = Utility.FormatBytes(Main.rxData);
                var bytesSent = Utility.FormatBytes(Main.txData);
                return Main.time <= 0 ? "not connected" : $"{bytesReceived}/s in, {bytesSent}/s out";
            };

            Interface.Oxide.ServerConsole.Status3Left = () => DateTime.Today.AddSeconds(Main.mapTime).ToString("h:mm tt").ToLower();
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {Main.versionNumber}";
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
