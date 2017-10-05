using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.MedievalEngineers.Libraries.Covalence;

namespace Oxide.Game.MedievalEngineers
{
    /// <summary>
    /// The core Medieval Engineers plugin
    /// </summary>
    public partial class MedievalEngineersCore : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the MedievalEngineersCore class
        /// </summary>
        public MedievalEngineersCore()
        {
            // Set plugin info attributes
            Title = "Medieval Engineers";
            Author = "Oxide Team";
            var assemblyVersion = MedievalEngineersExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        // Libraries
        //internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        //internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly MedievalEngineersCovalenceProvider Covalence = MedievalEngineersCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = Covalence.CreateServer();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        private bool serverInitialized;

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private bool PermissionsLoaded(IPlayer player)
        {
            if (permission.IsLoaded) return true;
            player.Reply(lang.GetMessage("PermissionsNotLoaded", this, player.Id), permission.LastException.Message);
            return false;
        }

        #endregion

        #region Core Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("game version", Server.Version);

            // Add core plugin commands
            AddCovalenceCommand(new[] { "oxide.plugins", "plugins" }, "PluginsCommand", "oxide.plugins");
            AddCovalenceCommand(new[] { "oxide.load", "load" }, "LoadCommand", "oxide.load");
            AddCovalenceCommand(new[] { "oxide.reload", "reload" }, "ReloadCommand", "oxide.reload");
            AddCovalenceCommand(new[] { "oxide.unload", "unload" }, "UnloadCommand", "oxide.unload");

            // Add core permission commands
            AddCovalenceCommand(new[] { "oxide.grant", "grant" }, "GrantCommand", "oxide.grant");
            AddCovalenceCommand(new[] { "oxide.group", "group" }, "GroupCommand", "oxide.group");
            AddCovalenceCommand(new[] { "oxide.revoke", "revoke" }, "RevokeCommand", "oxide.revoke");
            AddCovalenceCommand(new[] { "oxide.show", "show" }, "ShowCommand", "oxide.show");
            AddCovalenceCommand(new[] { "oxide.usergroup", "usergroup" }, "UserGroupCommand", "oxide.usergroup");

            // Add core misc commands
            AddCovalenceCommand(new[] { "oxide.lang", "lang" }, "LangCommand");
            AddCovalenceCommand(new[] { "oxide.version", "version" }, "VersionCommand");

            // Register messages for localization
            foreach (var language in Core.Localization.languages) lang.RegisterMessages(language.Value, this, language.Key);

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

        /// <summary>
        /// Called when another plugin has been loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            // Call OnServerInitialized for hotloaded plugins
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;

            Analytics.Collect();
            MedievalEngineersExtension.ServerConsole();

            serverInitialized = true;
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion
    }
}
