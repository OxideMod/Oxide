using System;
using System.Linq;

using Facepunch;
using Network;
using Rust;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Game.Rust
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class RustExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Rust";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "Assembly-CSharp-firstpass", "DestMath", "Facepunch.Network", "Facepunch.System", "Facepunch.UnityEngine",
            "mscorlib", "Oxide.Core", "Oxide.Game.Rust", "protobuf-net", "RustBuild", "Rust.Data", "System", "System.Core", "UnityEngine"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "ConVar", "Dest", "Facepunch", "Network", "Oxide.Game.Rust.Cui", "ProtoBuf", "PVT", "Rust", "Steamworks", "System.Collections",
            "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
            "AngryAnt Behave version",
            "Failed to load plugin '1' (no source found)",
            "HDR RenderTexture format is not supported on this platform.",
            "Image Effects are not supported on this platform.",
            "Missing projectileID",
            "The image effect Main Camera",
            "The image effect effect -",
            "Unable to find shaders",
            "Unsupported encoding: 'utf8'",
            "[AmplifyColor]",
            "[AmplifyOcclusion]",
            "[SpawnHandler] populationCounts"
        };

        /// <summary>
        /// Initializes a new instance of the RustExtension class
        /// </summary>
        /// <param name="manager"></param>
        public RustExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new RustPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("Rust", new Libraries.Rust());
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
            if (!Interface.Oxide.EnableConsole()) return;

            Output.OnMessage += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (!input.Contains(".")) input = string.Concat("global.", input);
                return ConsoleSystem.Index.GetAll().Where(c => c.namefull.StartsWith(input.ToLower())).ToList().ConvertAll(c => c.namefull).ToArray();
            };
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{BasePlayer.activePlayerList.Count} | {ConVar.Server.hostname}";
            Interface.Oxide.ServerConsole.Status1Left = () => $" {ConVar.Server.hostname}";
            Interface.Oxide.ServerConsole.Status1Right = () => $"{Performance.frameRate}fps, {Number.FormatSeconds((ulong)UnityEngine.Time.realtimeSinceStartup)}";
            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = BasePlayer.activePlayerList.Count;
                var playerLimit = ConVar.Server.maxplayers;
                var sleeperCount = BasePlayer.sleepingPlayerList.Count;
                var sleepers = sleeperCount + (sleeperCount.Equals(1) ? " sleeper" : " sleepers");
                var entitiesCount = BaseNetworkable.serverEntities.Count;
                var entities = entitiesCount + (entitiesCount.Equals(1) ? " entity" : " entities");
                return string.Concat(" ", players, "/", playerLimit, " players, ", sleepers, ", ", entities);
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (Net.sv == null || !Net.sv.IsConnected()) return "not connected";
                var inbound = Utility.FormatBytes(Net.sv.GetStat(null, NetworkPeer.StatTypeLong.BytesReceived_LastSecond));
                var outbound = Utility.FormatBytes(Net.sv.GetStat(null, NetworkPeer.StatTypeLong.BytesSent_LastSecond));
                return string.Concat(inbound, "/s in, ", outbound, "/s out");
            };
            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var gameTime = (!TOD_Sky.Instance ? DateTime.Now : TOD_Sky.Instance.Cycle.DateTime).ToString("h:mm tt").ToLower();
                return $" {gameTime}, {ConVar.Server.level} [{ConVar.Server.worldsize}, {ConVar.Server.seed}]";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {BuildInformation.VersionStampDays} ({Protocol.network})";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            if (!string.IsNullOrEmpty(input)) ConsoleSystem.Run.Server.Normal(input);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
            {
                color = ConsoleColor.Yellow;
                ConVar.Server.Log("Log.Warning.txt", message);
            }
            else if (type == LogType.Error)
            {
                color = ConsoleColor.Red;
                ConVar.Server.Log("Log.Error.txt", message);
            }
            else if (type == LogType.Exception)
            {
                color = ConsoleColor.Red;
                ConVar.Server.Log("Log.Exception.txt", message);
            }
            else if (type == LogType.Assert)
            {
                color = ConsoleColor.Red;
                ConVar.Server.Log("Log.Assert.txt", message);
            }
            else if (!message.StartsWith("[CHAT]"))
                ConVar.Server.Log("Log.Log.txt", message);
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
