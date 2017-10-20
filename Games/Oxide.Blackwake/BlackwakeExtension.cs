using System;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using UnityEngine;

namespace Oxide.Game.Blackwake
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class BlackwakeExtension : Extension
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
        public override string Name => "Blackwake";

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
            "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
            "[TeamSelect] player SERVER joined",
            "BoxColliders does not support negative scale or size.",
            "Current environment:",
            "Parent of RectTransform is being set with parent property",
            "Player added to list SERVER",
            "The image effect",
            "audioclips found",
            "meshes found",
            "setting 640x480"
        };

        /// <summary>
        /// Initializes a new instance of the BlackwakeExtension class
        /// </summary>
        /// <param name="manager"></param>
        public BlackwakeExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new BlackwakePluginLoader());
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
            Application.logMessageReceived += HandleLog;
            if (Interface.Oxide.EnableConsole()) Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            /*var maxPlayers = 0;
            switch (FCNGAAPKKEO.CPEDGJLHPFP)
            {
                case 1:
                case 3:
                case 4:
                    maxPlayers = 16;
                    break;
                case 2:
                    maxPlayers = 30;
                    break;
                case 5:
                    maxPlayers = 32;
                    break;
                case 6:
                    maxPlayers = 52;
                    break;
                case 7:
                    maxPlayers = 54;
                    break;
            }

            Interface.Oxide.ServerConsole.Title = () => $"{GameMode.KGGCHDAHENK.FDHEIBCAAOH.Count() - 1} | {FCNGAAPKKEO.MHBDLHCODIH}";

            Interface.Oxide.ServerConsole.Status1Left = () => FCNGAAPKKEO.MHBDLHCODIH;*/
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            //Interface.Oxide.ServerConsole.Status2Left = () => $"{GameMode.KGGCHDAHENK.FDHEIBCAAOH.Count() - 1}/{maxPlayers} players";
            /*Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (!NetUtils.NManager.isNetworkActive) return "not connected";

                var bytesReceived = 0;
                var bytesSent = 0;
                foreach (var connection in NetUtils.GetMembers())
                {
                    int unused;
                    int statsOut;
                    int statsIn;
                    //connection.GetStatsIn(out unused, out statsIn);
                    //connection.GetStatsOut(out unused, out unused, out statsOut, out unused);
                    //bytesReceived += statsIn;
                    //bytesSent += statsOut;
                }
                return $"{Utility.FormatBytes(bytesReceived)}/s in, {Utility.FormatBytes(bytesSent)}/s out";
            };*/

            /*Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var map = SceneManager.GetActiveScene().name == "Menu" ? "Lobby" : SceneManager.GetActiveScene().name;
                return $"{map} [{GlobalManager.I.GameMode.Name.ToLower()}, ?? lives]";
            };*/
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version}"/* for {SteamAuth.NPCPMKJLAJN()}"*/;
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Implement when possible
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
