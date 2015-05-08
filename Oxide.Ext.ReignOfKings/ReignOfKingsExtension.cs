using System;
using System.Collections.Generic;
using System.Reflection;

using CodeHatch.Build;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;

using Oxide.Core;
using Oxide.Core.Extensions;

using Oxide.ReignOfKings.Libraries;
using Oxide.ReignOfKings.Plugins;

using UnityEngine;

namespace Oxide.ReignOfKings
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class ReignOfKingsExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name { get { return "ReignOfKings"; } }

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version { get { return new VersionNumber(1, 0, OxideMod.Version.Patch); } }

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author { get { return "Oxide Team"; } }

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
        /// <param name="manager"></param>
        public override void Load()
        {
            IsGameExtension = true;

            // Register our loader
            Manager.RegisterPluginLoader(new ReignOfKingsPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("ROK", new Libraries.ReignOfKings());
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
            Logger.LogToFile = false;
            Application.logMessageReceived += HandleLog;
            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
            Interface.Oxide.ServerConsole.Status1Left = () => string.Concat("Game Time: ", GameClock.Instance.TimeOfDayAsClockString(), " Weather: ", Weather.Instance.CurrentWeather);
            Interface.Oxide.ServerConsole.Status1Right = () => string.Concat("Players: ", Server.PlayerCount, "/", Server.PlayerLimit, " Frame Rate: ", Mathf.RoundToInt(1f / Time.smoothDeltaTime), " FPS");
            Interface.Oxide.ServerConsole.Status2Left = () => string.Concat("Version: ", GameInfo.VersionString, "(", GameInfo.Version, ") - ", GameInfo.VersionName);
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                var players = Server.AllPlayers;
                double bytesSent = 0;
                double bytesReceived = 0;
                for (var i = 0; i < players.Count; i++)
                {
                    var statistics = players[i].Connection.Statistics;
                    bytesSent += statistics.BytesSentPerSecond;
                    bytesReceived += statistics.BytesReceivedPerSecond;
                }
                return string.Concat("Total Sent: ", bytesSent, " B/s Total Receive: ", bytesReceived, " B/s");
            };
        }

        private void ServerConsoleOnInput(string input)
        {
            var messages = (List<Console.Message>)typeof(Console).GetField("m_messages", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            messages.Clear();
            if (CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, input))
            {
                Interface.Oxide.ServerConsole.AddMessage(Console.CurrentOutput);
            }
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
