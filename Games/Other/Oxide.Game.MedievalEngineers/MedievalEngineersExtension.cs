using System;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using VRage.Game;

namespace Oxide.Game.MedievalEngineers
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class MedievalEngineersExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "MedievalEngineers";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

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
        /// Initializes a new instance of the MedievalEngineersExtension class
        /// </summary>
        /// <param name="manager"></param>
        public MedievalEngineersExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load() => Manager.RegisterPluginLoader(new MedievalEngineersPluginLoader());

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

            Interface.Oxide.ServerConsole.Title = () => $"{MyMultiplayer.Static?.MemberCount} | {MySandboxGame.ConfigDedicated.ServerName}";
            Interface.Oxide.ServerConsole.Status1Left = () => MySandboxGame.ConfigDedicated.ServerName;
            /*Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Sandbox.Engine.Utils.MyFpsManager.GetFps();
                var seconds = TimeSpan.FromSeconds(??);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime); // MySession.Static.ElapsedGameTime // MySandboxGame.TotalTimeInMilliseconds
            };*/
            Interface.Oxide.ServerConsole.Status2Left = () => $"{MyMultiplayer.Static?.MemberCount}/{MyMultiplayer.Static?.MemberLimit}";
            /*Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                var bytesReceived = Utility.FormatBytes(Main.rxData);
                var bytesSent = Utility.FormatBytes(Main.txData);
                return Main.time <= 0 ? "0b/s in, 0b/s out" : string.Concat(bytesReceived, "/s in, ", bytesSent, "/s out");
            };*/
            /*Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = DateTime.Today.AddSeconds(Main.mapTime).ToString("h:mm tt").ToLower();
                return string.Concat(" ", time); // TODO: More info
            };*/
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {MyFinalBuildConstants.GAME_VERSION}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO: Handle console input
        }
    }
}
