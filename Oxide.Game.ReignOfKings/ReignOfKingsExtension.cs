using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CodeHatch.Build;
using CodeHatch.Engine.Administration;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using CodeHatch.Sockets;

using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Game.ReignOfKings.Libraries;

using UnityEngine;

namespace Oxide.Game.ReignOfKings
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class ReignOfKingsExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "ReignOfKings";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine" };
        public override string[] WhitelistNamespaces => new[] { "CodeHatch", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine" };

        internal static readonly string[] Filter =
        {
            "9999999999 has null AuthenticationKey!",
            "<color=magenta>[Entity]",
            "<color=yellow>Specific",
            "<color=yellow>Transform",
            "A collider was found on a gameobject",
            "An error occured handling",
            "Cannot retrieve Entity because the component",
            "Client owned object was not found to sync with id",
            "Could not attach to bone of type",
            "Could not find any serialized data",
            "Could not find the bone",
            "Could not initialize the native Steamworks API",
            "Could not retrieve Entity with Network View ID",
            "Could not use FastAO because:",
            "Could not use effect because",
            "Dedicated mode detected.",
            "Destroying DisableWithDistance",
            "Destroying self because the given entity is",
            "Failed to apply setting to DrawDistanceQuality",
            "Flow controller warning:",
            "FORM IS UnityEngine.WWWForm",
            "HDR RenderTexture",
            "HDR and MultisampleAntiAliasing",
            "Instantiating Base and Dedicated",
            "IT WORKED???",
            "Load Server GUID:",
            "Loading: ",
            "Lobby query failed.",
            "Maximum number of connections can't be higher than",
            "No AudioListener found in the scene",
            "Otherwise billboarding/lighting will not work correctly",
            "PlayerTracker: Tracker",
            "Private RPC R was not sent",
            "Processing new connection...",
            "Registering user 9999999999 with authkey",
            "Registering... Success",
            "Save Server GUID:",
            "Serialization settings set successfully",
            "Server has connected.",
            "Setting breakpad minidump AppID",
            "Standard Deviation:",
            "SteamInitializeFailed",
            "Steam_SetMinidumpSteamID:",
            "Sync member value was null",
            "The exclusive layer does not match the requested layer",
            "The game ended because an error occured",
            "The image effect Main Camera",
            "The referenced script on this Behaviour is missing!",
            "There were some issues with the attached",
            "This could be due to momentary deregistration",
            "Trying to read past the buffer size",
            "[EAC] [Debug]",
            "[EAC] [Info]",
            "[EAC] [Warning]",
            "[WARNING] Recieved a",
            "\"string button\" is empty;",
            "armorManager == null",
            "cannot estimate angular velocity.",
            "eac_server.dll",
            "is missing a default constructor.",
            "linkedTo == null",
            "m_guiCamera == null",
            "melee == null",
            "in ServerLobbyModule",
            "online is True",
            "this is a local server",
            "with authkey System.Byte[]"
        };

        private static readonly FieldInfo SocketServerField = typeof (SocketAdminConsole).GetField("_server", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo MessagesField = typeof (Console).GetField("m_messages", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// Initializes a new instance of the ReignOfKingsExtension class
        /// </summary>
        /// <param name="manager"></param>
        public ReignOfKingsExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new ReignOfKingsPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("RoK", new Libraries.ReignOfKings());
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
            if (!Interface.Oxide.CheckConsole()) return;

            var socketAdminConsole = UnityEngine.Object.FindObjectOfType<SocketAdminConsole>();
            var socketServer = (SocketServer) SocketServerField.GetValue(socketAdminConsole);
            if (socketServer.Clients.Count > 0) return;
            socketAdminConsole.enabled = false;

            if (!Interface.Oxide.EnableConsole()) return;

            Application.logMessageReceived += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;

            Interface.Oxide.ServerConsole.Title = () =>
            {
                var players = Server.PlayerCount;
                var hostname = DedicatedServerBypass.Settings.ServerName;
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                var hostname = DedicatedServerBypass.Settings.ServerName;
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
                var players = Server.PlayerCount;
                var playerLimit = Server.PlayerLimit;
                var sleepersCount = CodeHatch.StarForge.Sleeping.PlayerSleeperObject.AllSleeperObjects.Count;
                var sleepers = sleepersCount + (sleepersCount.Equals(1) ? " sleeper" : " sleepers");
                var entitiesCount = CodeHatch.Engine.Core.Cache.Entity.GetAll().Count;
                var entities = entitiesCount + (entitiesCount.Equals(1) ? " entity" : " entities");
                return string.Concat(" ", players, "/", playerLimit, " players, ", sleepers, ", ", entities);
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (uLink.Network.time <= 0) return "0b/s in, 0b/s out";
                var players = Server.AllPlayers;
                double bytesSent = 0;
                double bytesReceived = 0;
                foreach (var player in players)
                {
                    if (!player.Connection.IsConnected) continue;
                    var statistics = player.Connection.Statistics;
                    bytesSent += statistics.BytesSentPerSecond;
                    bytesReceived += statistics.BytesReceivedPerSecond;
                }
                return string.Concat(FormatBytes(bytesReceived), "/s in, ", FormatBytes(bytesSent), "/s out");
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var gameTime = GameClock.Instance != null ? GameClock.Instance.TimeOfDayAsClockString() : "Unknown";
                var weather = Weather.Instance != null ? Weather.Instance.CurrentWeather.ToString() : "Unknown";
                return string.Concat(" ", gameTime, ", Weather: ", weather);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = GameInfo.VersionName;
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;

            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (input.StartsWith("/")) input = input.Remove(0, 1);
                return CommandManager.RegisteredCommands.Keys.Where(c => c.StartsWith(input.ToLower())).ToArray();
            };
        }

        private static string FormatBytes(double bytes)
        {
            string type;
            if (bytes > 1024*1024)
            {
                type = "mb";
                bytes /= (1024*1024);
            }
            else if (bytes > 1024)
            {
                type = "kb";
                bytes /= 1024;
            }
            else
                type = "b";
            return $"{bytes:0}{type}";
        }

        private void ServerConsoleOnInput(string input)
        {
            if (!input.StartsWith("/")) input = "/" + input;
            var messages = (List<Console.Message>) MessagesField.GetValue(null);
            messages.Clear();
            if (!CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, input)) return;
            var output = Console.CurrentOutput.TrimEnd('\n', '\r');
            if (!string.IsNullOrEmpty(output)) Interface.Oxide.ServerConsole.AddMessage(output);
        }

        private void HandleLog(string message, string stackTrace, LogType type)
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
