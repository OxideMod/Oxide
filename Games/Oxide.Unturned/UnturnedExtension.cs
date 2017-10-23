using System;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Version = System.Version;

namespace Oxide.Game.Unturned
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class UnturnedExtension : Extension
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
        public override string Name => "Unturned";

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
            "Assembly-CSharp", "mscorlib", "System", "System.Core", "UnityEngine"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
            "The image effect Camera"
        };

        /// <summary>
        /// Initializes a new instance of the UnturnedExtension class
        /// </summary>
        /// <param name="manager"></param>
        public UnturnedExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new UnturnedPluginLoader());
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
            // Limit FPS to reduce CPU usage
            Application.targetFrameRate = 256;

            if (!Interface.Oxide.EnableConsole()) return;

            Application.logMessageReceived += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{Provider.clients.Count} | {Provider.serverName}";

            Interface.Oxide.ServerConsole.Status1Left = () => Provider.serverName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{Provider.clients.Count}/{Provider.maxPlayers} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                var bytesReceived = Utility.FormatBytes(Provider.bytesReceived);
                var bytesSent = Utility.FormatBytes(Provider.bytesSent);
                return Provider.time <= 0 ? "0b/s in, 0b/s out" : $"{bytesReceived}/s in, {bytesSent}/s out";
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var gameTime = DateTime.Today.AddSeconds(LightingManager.time * 120).ToString("h:mm tt");
                return $"{gameTime.ToLower()}, {Provider.map ?? "Unknown"}";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {Provider.APP_VERSION}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            input = input.Trim();
            if (!string.IsNullOrEmpty(input)) Commander.execute(CSteamID.Nil, input);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

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
