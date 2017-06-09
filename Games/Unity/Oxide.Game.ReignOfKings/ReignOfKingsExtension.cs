﻿using System;
using System.Linq;
using System.Reflection;
using CodeHatch.Build;
using CodeHatch.Common;
using CodeHatch.Engine.Administration;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using Oxide.Game.ReignOfKings.Libraries;
using UnityEngine;

namespace Oxide.Game.ReignOfKings
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class ReignOfKingsExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "ReignOfKings";

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
            "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "CodeHatch", "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
            "9999999999 has null AuthenticationKey!",
            "<color=magenta>[Entity]",
            "<color=yellow>Specific",
            "<color=yellow>Transform",
            "A collider was found on a gameobject",
            "An error occured handling",
            "Cannot find any Entities.",
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
            "Finished loading...",
            "Flow controller warning:",
            "FORM IS UnityEngine.WWWForm",
            "Game has started.",
            "Handling user status UserAuthenticated:",
            "HDR RenderTexture",
            "HDR and MultisampleAntiAliasing",
            "Instantiating Base and Dedicated",
            "ItemCacheServiceServer is overwriting a live item cache.",
            "IT WORKED???",
            "Load Server GUID:",
            "Loading: ",
            "Lobby query failed.",
            "Maximum number of connections can't be higher than",
            "Multiple audio items with name",
            "No AudioListener found in the scene",
            "Otherwise billboarding/lighting will not work correctly",
            "PlayerTracker: Tracker",
            "Private RPC R was not sent",
            "Processing new connection...",
            "Registering user",
            "Registering... Success",
            "Retrieving BiomeMap from Thrones Terrain",
            "Save Server GUID:",
            "Serialization settings set successfully",
            "Server has connected.",
            "ServerLobbyModule.cs - WWW - Result failure:",
            "Setting breakpad minidump AppID",
            "Standard Deviation:",
            "SteamInitializeFailed",
            "Steam_SetMinidumpSteamID:",
            "Sync member value was null",
            "The contents affected could not be found:",
            "The exclusive layer does not match the requested layer",
            "The game ended because an error occured",
            "The image effect Main Camera",
            "The referenced script on this Behaviour is missing!",
            "There were some issues with the attached",
            "This could be due to momentary deregistration",
            "Trying to read past the buffer size",
            "Unregistering user",
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

        /// <summary>
        /// Initializes a new instance of the ReignOfKingsExtension class
        /// </summary>
        /// <param name="manager"></param>
        public ReignOfKingsExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterPluginLoader(new ReignOfKingsPluginLoader());
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
            if (!Interface.Oxide.CheckConsole() || !Interface.Oxide.EnableConsole()) return;

            var socketAdminConsole = UnityEngine.Object.FindObjectOfType<SocketAdminConsole>();
            if (socketAdminConsole._server.Clients.Count > 0) return;
            socketAdminConsole.enabled = false;

            Application.logMessageReceived += HandleLog;
            Logger.DebugLogged += HandleLog;
            Logger.InfoLogged += HandleLog;
            Logger.WarningLogged += HandleLog;
            Logger.ErrorLogged += HandleLog;
            Logger.ExceptionLogged += HandleLog;
            Application.logMessageReceived += (message, stackTrace, type) =>
            {
                if (type == LogType.Exception) Interface.Oxide.LogDebug(message + "\n" + stackTrace);
            };

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (input.StartsWith("/")) input = input.Remove(0, 1);
                return CommandManager.RegisteredCommands.Keys.Where(c => c.StartsWith(input.ToLower())).ToArray();
            };
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{Server.PlayerCount} | {DedicatedServerBypass.Settings.ServerName}";

            Interface.Oxide.ServerConsole.Status1Left = () => DedicatedServerBypass.Settings.ServerName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var time = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{time.TotalHours:00}h{time.Minutes:00}m{time.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{Mathf.RoundToInt(1f / Time.smoothDeltaTime)}fps, {uptime}";
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                var players = $"{Server.PlayerCount}/{Server.PlayerLimit} players";
                var sleepers = CodeHatch.StarForge.Sleeping.PlayerSleeperObject.AllSleeperObjects.Count;
                var entities = CodeHatch.Engine.Core.Cache.Entity.GetAll().Count;
                return $"{players}, {sleepers + (sleepers.Equals(1) ? " sleeper" : " sleepers")}, {entities + (entities.Equals(1) ? " entity" : " entities")}";
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (uLink.NetworkTime.serverTime <= 0) return "not connected";

                double bytesReceived = 0;
                double bytesSent = 0;
                foreach (var player in Server.AllPlayers)
                {
                    if (!player.Connection.IsConnected) continue;

                    var statistics = player.Connection.Statistics;
                    bytesReceived += statistics.BytesReceivedPerSecond;
                    bytesSent += statistics.BytesSentPerSecond;
                }
                return $"{Utility.FormatBytes(bytesReceived)}/s in, {Utility.FormatBytes(bytesSent)}/s out";
            };

            Interface.Oxide.ServerConsole.Status3Left = () => $"{GameClock.Instance.TimeOfDayAsClockString()}, Weather: {Weather.Instance.CurrentWeather}";
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {GameInfo.VersionString} ({GameInfo.VersionName})";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private static void ServerConsoleOnInput(string input)
        {
            if (!input.StartsWith("/")) input = "/" + input;
            Console.Messages.Clear();
            if (!CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, input)) return;
            var output = Console.CurrentOutput.TrimEnd('\n', '\r');
            if (!string.IsNullOrEmpty(output)) Interface.Oxide.ServerConsole.AddMessage(output);
        }

        private static void HandleLog(Exception message, object context, Type type)
        {
            HandleLog(message.ToString(), IDUtil.GetObjectIDString(context), LogType.Error);
        }

        private static void HandleLog(string message, object context, Type type)
        {
            HandleLog(message, IDUtil.GetObjectIDString(context), LogType.Log);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

            var color = ConsoleColor.Gray;
            var remoteType = "generic";

            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                color = ConsoleColor.Red;

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
