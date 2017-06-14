﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.RemoteConsole;
using Oxide.Plugins;
using Sandbox;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using VRage.Game;

namespace Oxide.Game.SpaceEngineers
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class SpaceEngineersExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "SpaceEngineers";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        internal static readonly HashSet<string> DefaultReferences = new HashSet<string>
        {
        };

        public override string[] WhitelistAssemblies => new[]
        {
            "mscorlib", "Oxide.Core", "System", "System.Core"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "System.Collections", "System.Security.Cryptography", "System.Text"
        };

        public static string[] Filter =
        {
        };

        /// <summary>
        /// Initializes a new instance of the SpaceEngineersExtension class
        /// </summary>
        /// <param name="manager"></param>
        public SpaceEngineersExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterLibrary("Command", new Libraries.Command());
            Manager.RegisterLibrary("Player", new Libraries.Player());
            Manager.RegisterLibrary("Server", new Libraries.Server());
            Manager.RegisterPluginLoader(new SpaceEngineersPluginLoader());

            Interface.Oxide.RegisterEngineClock(() => MySandboxGame.Static != null ? MySandboxGame.TotalTimeInMilliseconds / 1000.0f : 0f);
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public override void LoadPluginWatchers(string pluginDirectory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            CSharpPluginLoader.PluginReferences.UnionWith(DefaultReferences);
            if (Interface.Oxide.EnableConsole()) Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;

            // TODO: Add console log handling

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            Interface.Oxide.ServerConsole.Title = () => $"{MyMultiplayer.Static?.MemberCount - 1} | {MySandboxGame.ConfigDedicated.ServerName}";
            Interface.Oxide.ServerConsole.Status1Left = () => MySandboxGame.ConfigDedicated.ServerName;
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                //var fps = Sandbox.Engine.Utils.MyFpsManager.GetFps();
                var fps = MyPhysics.SimulationRatio;
                var seconds = MySession.Static.ElapsedPlayTime;
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return $"{fps.ToString("0.00")}, {uptime}"; // MySession.Static.ElapsedGameTime // MySandboxGame.TotalTimeInMilliseconds
            };
            Interface.Oxide.ServerConsole.Status2Left = () => $"{MyMultiplayer.Static?.MemberCount - 1}/{MyMultiplayer.Static?.MemberLimit}";
            /*Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                var bytesReceived = Utility.FormatBytes(MyHud.Netgraph.LastPacketBytesReceived);
                var bytesSent = Utility.FormatBytes(MyHud.Netgraph.LastPacketBytesSent);
                return MySession.Static.ElapsedPlayTime.TotalSeconds <= 0 ? "0b/s in, 0b/s out" : string.Concat(bytesReceived, "/s in, ", bytesSent, "/s out");
            };*/
            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = MySession.Static.InGameTime.ToString("h:mm tt").ToLower();
                return $"{time}, {MySession.Static.Name ?? "Unknown"}";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {MyFinalBuildConstants.APP_VERSION.FormattedText.Replace('_', '.')}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        internal static void ServerConsoleOnInput(string input)
        {
            //if (!string.IsNullOrEmpty(input)) ConsoleManager.Instance.ExecuteCommand(input);
            if (input.ToLower().Equals("quit") || input.ToLower().Equals("shutdown")) MySandboxGame.ExitThreadSafe();
        }

        private static void HandleLog(string message, string stackTrace)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.Contains)) return;

            var color = ConsoleColor.Gray;
            var remoteType = "generic";

            // TODO: Color handling

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
