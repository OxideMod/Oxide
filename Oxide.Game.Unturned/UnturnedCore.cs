using System;
using System.Linq;
using System.Reflection;

using SDG.Unturned;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Game.Unturned
{
    /// <summary>
    /// The core Unturned plugin
    /// </summary>
    public class UnturnedCore : CSPlugin
    {
        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        /// <summary>
        /// Initializes a new instance of the UnturnedCore class
        /// </summary>
        public UnturnedCore()
        {
            // Set attributes
            Name = "unturnedcore";
            Title = "Unturned Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>("Plugins");
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "unturned");
            RemoteLogger.SetTag("protocol", Provider.APP_VERSION);
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", Provider.serverName);
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
        /// Called when the CommandWindow is instantiated
        /// </summary>
        [HookMethod("IOnCommandWindow")]
        private bool IOnCommandWindow()
        {
            var commandWindowField = typeof (Provider).GetField("commandWindow", BindingFlags.Static | BindingFlags.NonPublic);
            Interface.Oxide.NextTick(() =>
            {
                commandWindowField?.SetValue(null, null);
            });
            return false;
        }

        /// <summary>
        /// Called when the CommandWindow.Log is called
        /// </summary>
        [HookMethod("IOnLog")]
        private void IOnLog(object text, ConsoleColor color)
        {
            var message = text?.ToString();
            if (string.IsNullOrEmpty(message) || UnturnedExtension.Filter.Any(message.StartsWith)) return;
            Interface.Oxide.ServerConsole.AddMessage(text.ToString(), color);
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore")
                InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }
    }
}
