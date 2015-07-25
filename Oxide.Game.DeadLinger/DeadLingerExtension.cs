using System;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Extensions;

using UnityEngine;

namespace Oxide.Game.DeadLinger
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class DeadLingerExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "DeadLinger";

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

        private static readonly MethodInfo EvalInputString = typeof (DebugConsole).GetMethod("EvalInputString", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly string[] Filter =
        {
            "CategoryReader Awake",
            "Content Template count:",
            "Duplicate category name",
            "Duplicate entity name in the entity file:",
            "Duplicate template name:",
            "Entities files count:",
            "Exception loading an entity definition.",
            "Failed to auto generate building waypoint links",
            "HDR RenderTexture format",
            "LoadAllCategories()",
            "Needs a desktop tdlDir.txt",
            "Template:",
            "The image effect",
            "Wardrobe file count:",
            "Wardrobe:",
            "attr class =",
            "attr name =",
            "attr prefab =",
            "attr ilod =",
            "attr ilod_range =",
            "last entity name parsed:",
            "loadCategoryFile",
            "prefab load"
        };

        /// <summary>
        /// Initializes a new instance of the DeadLingerExtension class
        /// </summary>
        /// <param name="manager"></param>
        public DeadLingerExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new DeadLingerPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("DeadLinger", new Libraries.DeadLinger());
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
            Application.LoadLevel("mainScene");
            // TODO: Add status information
        }

        private static void ServerConsoleOnInput(string input)
        {
            EvalInputString.Invoke(DebugConsole.Singleton, new object[] { input });
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
