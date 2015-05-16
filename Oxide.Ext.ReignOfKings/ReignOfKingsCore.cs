using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.ReignOfKings.Libraries;

using CodeHatch.Build;
using CodeHatch.Networking.Events.Players;

using UnityEngine;

namespace Oxide.ReignOfKings.Plugins
{
    /// <summary>
    /// The core Reign of Kings plugin
    /// </summary>
    public class ReignOfKingsCore : CSPlugin
    {
        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The command lib
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // Track when the server has been initialized
        private bool ServerInitialized;
        private bool LoggingInitialized;

        /// <summary>
        /// Initializes a new instance of the ReignOfKingsCore class
        /// </summary>
        public ReignOfKingsCore()
        {
            // Set attributes
            Name = "reignofkingscore";
            Title = "Reign of Kings Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.GetMod().GetLibrary<Core.Libraries.Plugins>("Plugins");
            if (plugins.Exists("unitycore")) InitializeLogging();

            // Cheat a reference for UnityEngine and uLink in the default plugin reference list
            var zero = Vector3.zero;
            var isServer = uLink.Network.isServer;
        }

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "reign of kings");
            RemoteLogger.SetTag("protocol", GameInfo.VersionName.ToLower());
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (ServerInitialized) return;
            ServerInitialized = true;
            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", DedicatedServerBypass.Settings.ServerName);
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown()
        {
            Interface.Oxide.OnShutdown();
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (ServerInitialized) plugin.CallHook("OnServerInitialized");
            if (!LoggingInitialized && plugin.Name == "unitycore")
                InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            LoggingInitialized = true;
            CallHook("InitLogging", null);
        }

        [HookMethod("OnLog")]
        private void OnLog(Logger.LogType logType, Type type, object message, object context)
        {
            if (Interface.Oxide.ServerConsole == null) return;
            var settings = Logger.GetSettings(type);
            var color = ConsoleColor.Gray;
            switch (logType)
            {
                case Logger.LogType.Exception:
                case Logger.LogType.Assert:
                case Logger.LogType.Error:
                    if (!settings.ShowError) return;
                    color = ConsoleColor.Red;
                    break;
                case Logger.LogType.Warning:
                    if (!settings.ShowWarning) return;
                    color = ConsoleColor.Yellow;
                    break;
                case Logger.LogType.Info:
                    if (!settings.ShowInfo) return;
                    break;
                case Logger.LogType.Debug:
                    if (!settings.ShowDebug) return;
                    break;
            }
            object obj = message as string;
            if (obj == null && message is Exception)
                obj = ((Exception) message).Message;
            var str = (string)obj;
            if (string.IsNullOrEmpty(str) || ReignOfKingsExtension.Filter.Any(str.Contains)) return;
            Interface.Oxide.ServerConsole.AddMessage(str, color);
        }

        /// <summary>
        /// Called when a chat command was run
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerCommand")]
        private object IOnPlayerCommand(PlayerCommandEvent e)
        {
            if (e == null || e.Player == null || e.Command == null) return null;

            string str = e.Command;
            if (str.Length == 0) return null;

            if (str[0] == '/')
            {
                // Get the message
                string message = str.Substring(1);

                // Parse it
                string cmd;
                string[] args;
                ParseChatCommand(message, out cmd, out args);
                if (cmd == null) return null;

                // handle it
                if (!cmdlib.HandleChatCommand(e.Player, cmd, args)) return null;

                // Handled
                return true;
            }

            // Default behavior
            return null;
        }

        /// <summary>
        /// Parses the specified chat command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseChatCommand(string argstr, out string cmd, out string[] args)
        {
            List<string> arglist = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inlongarg = false;
            for (int i = 0; i < argstr.Length; i++)
            {
                char c = argstr[i];
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        string arg = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                        sb = new StringBuilder();
                        inlongarg = false;
                    }
                    else
                    {
                        inlongarg = true;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inlongarg)
                {
                    string arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                    sb = new StringBuilder();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
            {
                string arg = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
            }
            if (arglist.Count == 0)
            {
                cmd = null;
                args = null;
                return;
            }
            cmd = arglist[0];
            arglist.RemoveAt(0);
            args = arglist.ToArray();
        }
    }
}
