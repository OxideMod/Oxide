using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CodeHatch.Build;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;

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
        public override string[] WhitelistNamespaces => new[] { "CodeHatch", "Steamworks", "System.Collections", "UnityEngine" };

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
            "HDR RenderTexture",
            "HDR and MultisampleAntiAliasing",
            "Instantiating Base and Dedicated",
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
            "ServerLobbyModule.cs",
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
            "eac_server.dll",
            "is missing a default constructor.",
            "linkedTo == null",
            "m_guiCamera == null",
            "melee == null",
            "with authkey System.Byte[]"
        };

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
            IsGameExtension = true;

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
            if (!Interface.Oxide.EnableConsole()) return;
            //Logger.ReloadSettings();
            Application.logMessageReceived += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Status1Left = () => string.Concat("Game Time: ", GameClock.Instance != null ? GameClock.Instance.TimeOfDayAsClockString() : "Unknown", " Weather: ", Weather.Instance != null ? Weather.Instance.CurrentWeather.ToString() : "Unknown");
            Interface.Oxide.ServerConsole.Status1Right = () => string.Concat("Players: ", Server.PlayerCount, "/", Server.PlayerLimit, " Frame Rate: ", Mathf.RoundToInt(1f / Time.smoothDeltaTime), " FPS");
            Interface.Oxide.ServerConsole.Status2Left = () => string.Concat("Version: ", GameInfo.VersionString, "(", GameInfo.Version, ") - ", GameInfo.VersionName);
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (uLink.Network.time <= 0) return "Total Sent: 0.0 B/s Total Receive: 0.0 B/s";
                var players = Server.AllPlayers;
                double bytesSent = 0;
                double bytesReceived = 0;
                for (var i = 0; i < players.Count; i++)
                {
                    if (!players[i].Connection.IsConnected) continue;
                    var statistics = players[i].Connection.Statistics;
                    bytesSent += statistics.BytesSentPerSecond;
                    bytesReceived += statistics.BytesReceivedPerSecond;
                }
                return $"Total Sent: {bytesSent:0.0} B/s Total Receive: {bytesReceived:0.0} B/s";
            };
            Interface.Oxide.ServerConsole.Title = () => string.Concat(Server.PlayerCount, " | ", DedicatedServerBypass.Settings.ServerName);
            Interface.Oxide.ServerConsole.Completion = input =>
            {
                if (string.IsNullOrEmpty(input)) return null;
                if (input.StartsWith("/")) input = input.Remove(0, 1);
                return CommandManager.RegisteredCommands.Keys.Where(c => c.StartsWith(input.ToLower())).ToArray();
            };
        }

        private void ServerConsoleOnInput(string input)
        {
            if (!input.StartsWith("/")) input = "/" + input;
            var messages = (List<Console.Message>)typeof(Console).GetField("m_messages", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            messages.Clear();
            if (CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, input))
            {
                Interface.Oxide.ServerConsole.AddMessage(Console.CurrentOutput.TrimEnd('\n', '\r'));
            }
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
