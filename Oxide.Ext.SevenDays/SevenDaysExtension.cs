using System;

using Oxide.Core;
using Oxide.Core.Extensions;

using Oxide.SevenDays.Plugins;

using UnityEngine;

namespace Oxide.SevenDays
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class SevenDaysExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "SevenDays"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, OxideMod.Version.Patch); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Oxide Team"; } }

        public override string[] WhitelistAssemblies { get { return new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine" }; } }
        public override string[] WhitelistNamespaces { get { return new[] { "Steamworks", "System.Collections", "UnityEngine" }; } }

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
        /// <param name="manager"></param>
        public override void Load()
        {
            IsGameExtension = true;

            // Register our loader
            Manager.RegisterPluginLoader(new SevenDaysPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("SevenDays", new Libraries.SevenDays());
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
        /// <param name="manager"></param>
        public override void OnModLoad()
        {
            if (!Interface.Oxide.EnableConsole()) return;
            Application.logMessageReceived += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Status1Left = () => string.Concat(GamePrefs.GetString(EnumGamePrefs.ServerName));
            //Interface.Oxide.ServerConsole.Status1Right = () => string.Concat("Players: ", Server.PlayerCount, "/", Server.PlayerLimit, " Frame Rate: ", Mathf.RoundToInt(1f / Time.smoothDeltaTime), " FPS");
            Interface.Oxide.ServerConsole.Status2Left = () => string.Concat("Version: ", cl000c.cCompatibilityVersion, ", Oxide: ", OxideMod.Version.ToString());
            //Interface.Oxide.ServerConsole.Status1Right = () => ();
        }

        private void ServerConsoleOnInput(string input)
        {
            var result = SingletonMonoBehaviour<SdtdConsole>.Instance.ExecuteSync(input, null);
            if (result != null) Interface.Oxide.ServerConsole.AddMessage(string.Join("\n", result.ToArray()));
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
