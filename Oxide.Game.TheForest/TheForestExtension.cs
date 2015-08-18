using System;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Extensions;

using UnityEngine;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class TheForestExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "TheForest";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine" };
        public override string[] WhitelistNamespaces => new[] { "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "TheForest", "UnityEngine" };

        private static readonly string[] Filter =
        {
            "****** Game Activation Sequence ******",
            "Body Variation",
            "CanResume:",
            "DestroyPickup:",
            "Game Activation Sequence step",
            "Hull (UnityEngine.GameObject)",
            "LobbyCreated param.m_eResult=k_EResult",
            "Refreshing Input Mapping Icons",
            "Skin Variation",
            "Skipped frame because",
            "Skipped rendering frame because",
            "WakeFromKnockOut",
            "attach: [",
            "delaying initial",
            "disableFlying",
            "going black",
            "null texture passed to GUI.DrawTexture",
            "planeCrash started",
            "setFemale",
            "setMale",
            "started steam server"
        };

        /// <summary>
        /// Initializes a new instance of the TheForestExtension class
        /// </summary>
        /// <param name="manager"></param>
        public TheForestExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new TheForestPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Forest", new Libraries.TheForest());
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

            Interface.Oxide.ServerConsole.Title = () =>
            {
                var players = CoopLobby.Instance?.MemberCount;
                var hostname = CoopLobby.Instance?.Info?.Name.Split("()".ToCharArray())[0];
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                var hostname = CoopLobby.Instance?.Info.Name.Split("()".ToCharArray())[0];
                return string.Concat(" ", hostname);
            };
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = CoopLobby.Instance?.MemberCount;
                var playerLimit = CoopLobby.Instance?.Info?.MemberLimit;
                return string.Concat(" ", players, "/", playerLimit, " players");
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                // TODO: Network in/out
                return "";
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                //var gameTime = TheForestAtmosphere.Instance?.TimeOfDay; // TODO: Fix NRE and format
                return string.Concat(" "/*, gameTime*/);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = "0.22"; // TODO: Grab version/protocol
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
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
