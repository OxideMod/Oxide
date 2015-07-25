using System;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Extensions;

using UnityEngine;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class SevenDaysExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "SevenDays";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine" };
        public override string[] WhitelistNamespaces => new[] { "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine" };

        private static readonly string[] Filter =
        {
            "Awake done",
            "Biomes image size",
            "Command line arguments:",
            "Dedicated server only build",
            "Exited thread thread_",
            "GamePref.",
            "GameStat.",
            "HDR Render",
            "HDR and MultisampleAntiAliasing",
            "INF Cleanup",
            "INF GMA.",
            "INF GOM.",
            "INF Disconnect",
            "INF WSD.",
            "Load key config",
            "Loading permissions file at",
            "NET: Starting server protocols",
            "NET: Stopping server protocols",
            "NET: Unity NW server",
            "POI image size",
            "Parsing server configfile:",
            "Persistent GamePrefs saved",
            "SaveAndCleanupWorld",
            "SelectionBoxManager.Instance:",
            "Setting breakpad minidump AppID",
            "StartAsServer",
            "StartGame",
            "INF OnApplicationQuit",
            "Started thread",
            "World.Cleanup",
            "World.Load:",
            "World.Unload",
            "WorldStaticData.Init()",
            "[NET] ServerShutdown",
            "[Steamworks.NET] NET: Server",
            "createWorld() done",
            "createWorld:"
        };

        /// <summary>
        /// Initializes a new instance of the SevenDaysExtension class
        /// </summary>
        /// <param name="manager"></param>
        public SevenDaysExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new SevenDaysPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("SDTD", new Libraries.SevenDays());
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
            Application.logMessageReceived += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Status1Left = () => string.Concat(GamePrefs.GetString(EnumGamePrefs.ServerName));
            //Interface.Oxide.ServerConsole.Status1Right = () => string.Concat("Players: ", Server.PlayerCount, "/", Server.PlayerLimit, " Frame Rate: ", Mathf.RoundToInt(1f / Time.smoothDeltaTime), " FPS");
            Interface.Oxide.ServerConsole.Status2Left = () => string.Concat("Version: ", cl000c.cCompatibilityVersion, ", Oxide: ", OxideMod.Version.ToString());
            //Interface.Oxide.ServerConsole.Status1Right = () => ();
            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                return SingletonMonoBehaviour<SdtdConsole>.Instance.commands.Keys.Where(c => c.StartsWith(input.ToLower())).ToArray();
            };
        }

        private static void ServerConsoleOnInput(string input)
        {
            var result = SingletonMonoBehaviour<SdtdConsole>.Instance.ExecuteSync(input, null);
            if (result != null) Interface.Oxide.ServerConsole.AddMessage(string.Join("\n", result.ToArray()));
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
