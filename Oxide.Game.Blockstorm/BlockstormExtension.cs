using System;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Extensions;

using UnityEngine;

namespace Oxide.Game.Blockstorm
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class BlockstormExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Blockstorm";

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

        };

        /// <summary>
        /// Initializes a new instance of the BlockstormExtension class
        /// </summary>
        /// <param name="manager"></param>
        public BlockstormExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new BlockstormPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("storm", new Libraries.Blockstorm());
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
            Application.RegisterLogCallback(HandleLog);
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            // TODO: Add status information
        }

        private static void ServerConsoleOnInput(string input)
        {
            // TODO
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;
            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
