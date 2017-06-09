﻿using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Game.FromTheDepths
{
    /// <summary>
    /// The core From the Depths plugin
    /// </summary>
    public class FromTheDepthsCore : CSPlugin
    {
        #region Initialization
 
        // Libraries
        //internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();

        // Instances
        //internal static readonly FromTheDepthsCovalenceProvider Covalence = FromTheDepthsCovalenceProvider.Instance;
        //internal static readonly IServer Server = Covalence.CreateServer();

        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        private bool serverInitialized;

        /// <summary>
        /// Initializes a new instance of the FromTheDepthsCore class
        /// </summary>
        public FromTheDepthsCore()
        {
            // Set plugin info attributes
            Title = "From the Depths";
            Author = "Oxide Team";
            var assemblyVersion = FromTheDepthsExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        #endregion

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("game version", StaticOptionsManager.version); // TODO: Use Covalence

            // Setup default permission groups
            if (permission.IsLoaded)
            {
                var rank = 0;
                foreach (var defaultGroup in Interface.Oxide.Config.Options.DefaultGroups)
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);

                permission.RegisterValidate(s =>
                {
                    ulong temp;
                    if (!ulong.TryParse(s, out temp)) return false;
                    var digits = temp == 0 ? 1 : (int)Math.Floor(Math.Log10(temp) + 1);
                    return digits >= 17;
                });

                permission.CleanUp();
            }
        }

        #endregion

        #region Server Hooks

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;
            serverInitialized = true;

            //Analytics.Collect(); // TODO: Uncomment once game has Covalence

            // Update server console window and status bars
            FromTheDepthsExtension.ServerConsole();
        }

        /// <summary>
        /// Called when the server is saving
        /// </summary>
        //[HookMethod("OnServerSave")]
        //private void OnServerSave() => Analytics.Collect();

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("IOnServerShutdown")]
        private void IOnServerShutdown()
        {
            Interface.Call("OnServerShutdown");
            Interface.Oxide.OnShutdown();
        }

        #endregion
    }
}
