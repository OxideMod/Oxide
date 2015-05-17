using System;
using Oxide.BoP.Plugins;
using Oxide.Core;
using Oxide.Core.Extensions;
using UnityEngine;

namespace Oxide.BoP
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class BoPExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "BoP"; } }

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
        /// Initializes a new instance of the BoPExtension class
        /// </summary>
        /// <param name="manager"></param>
        public BoPExtension(ExtensionManager manager)
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
            Manager.RegisterPluginLoader(new BoPPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("BoP", new Libraries.BoP());
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
            if (!Interface.Oxide.EnableConsole(true)) return;
            Application.logMessageReceived += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        private void ServerConsoleOnInput(string input)
        {
            //TODO
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
