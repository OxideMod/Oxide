using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Oxide.Game.GangBeasts
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class GangBeastsExtension : Extension
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
        public override string Name => "GangBeasts";

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
            "DontDestroyOnLoad only work for root GameObjects or components on root GameObjects",
            "Error: Global Illumination requires a graphics device to render albedo",
            "Failed to create agent because it is not close enough to the NavMesh",
            "Failed to load file from path: default/english",
            "Rewired: Found Xinput1_3.dll",
            "Rewired: Searching for compatible XInput library",
            "[FileIO] Could not load default/english from Resources",
            "improper JSON formatting:FileIO"
        };

        /// <summary>
        /// Initializes a new instance of the GangBeastsExtension class
        /// </summary>
        /// <param name="manager"></param>
        public GangBeastsExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new GangBeastsPluginLoader());
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
            CSharpPluginLoader.PluginReferences.UnionWith(DefaultReferences);

            if (!Interface.Oxide.EnableConsole()) return;

            Application.logMessageReceived += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        public static ServerConfig ServerConfig;

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            var gbConfig = UnityEngine.Object.FindObjectOfType<ServerConfig>();

            Interface.Oxide.ServerConsole.Title = () => $"{NetUtils.NManager.numPlayers} | {gbConfig.ServerName}"; // GetCurrentPlayerCount()

            Interface.Oxide.ServerConsole.Status1Left = () => gbConfig.ServerName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () => $"{NetUtils.NManager.numPlayers}/{NetUtils.NManager.maxConnections - 1} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (!NetUtils.NManager.isNetworkActive) return "not connected";

                var bytesReceived = 0;
                var bytesSent = 0;
                foreach (var connection in NetUtils.GetMembers())
                {
                    //int unused;
                    //int statsOut;
                    //int statsIn;
                    //connection.GetStatsIn(out unused, out statsIn);
                    //connection.GetStatsOut(out unused, out unused, out statsOut, out unused);
                    //bytesReceived += statsIn;
                    //bytesSent += statsOut;
                }
                return $"{Utility.FormatBytes(bytesReceived)}/s in, {Utility.FormatBytes(bytesSent)}/s out";
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var map = SceneManager.GetActiveScene().name == "Menu" ? "Lobby" : SceneManager.GetActiveScene().name;
                return $"{map} [{GlobalManager.I.GameMode.Name.ToLower()}, ?? lives]";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {GBConfigResourceLoader.ConfigData.GameVersion} ({GBConfigResourceLoader.ConfigData.NetworkVersion})";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Implement when possible
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
