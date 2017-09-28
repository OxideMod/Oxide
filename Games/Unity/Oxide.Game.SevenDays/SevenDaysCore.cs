using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.SevenDays.Libraries.Covalence;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// The core 7 Days to Die plugin
    /// </summary>
    public partial class SevenDaysCore : CSPlugin
    {
        #region Initialization
 
        /// <summary>
        /// Initializes a new instance of the SevenDaysCore class
        /// </summary>
        public SevenDaysCore()
        {
            // Set attributes
            Title = "7 Days to Die";
            Author = "Oxide Team";
            var assemblyVersion = SevenDaysExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        // Libraries
        //internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        //internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly SevenDaysCovalenceProvider Covalence = SevenDaysCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = Covalence.CreateServer();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        private bool serverInitialized;

        #endregion

        #region Localization

        private readonly Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"CommandUsageLoad", "Usage: load *|<pluginname>+"},
            {"CommandUsageGrant", "Usage: grant <group|user> <name|id> <permission>"},
            {"CommandUsageGroup", "Usage: group <add|remove|set> <name> [title] [rank]"},
            {"CommandUsageReload", "Usage: reload *|<pluginname>+"},
            {"CommandUsageRevoke", "Usage: revoke <group|user> <name|id> <permission>"},
            {"CommandUsageShow", "Usage: show <group|user> <name>\nUsage: show <groups|perms>"},
            {"CommandUsageUnload", "Usage: unload *|<pluginname>+"},
            {"CommandUsageUserGroup", "Usage: usergroup <add|remove> <username> <groupname>"},
            {"GroupAlreadyExists", "Group '{0}' already exists"},
            {"GroupChanged", "Group '{0}' changed"},
            {"GroupCreated", "Group '{0}' created"},
            {"GroupDeleted", "Group '{0}' deleted"},
            {"GroupNotFound", "Group '{0}' doesn't exist"},
            {"GroupParentChanged", "Group '{0}' parent changed to '{1}'"},
            {"GroupParentNotChanged", "Group '{0}' parent was not changed"},
            {"GroupParentNotFound", "Group parent '{0}' doesn't exist"},
            {"GroupPermissionGranted", "Group '{0}' granted permission '{1}'"},
            {"GroupPermissionRevoked", "Group '{0}' revoked permission '{1}'"},
            {"NoPluginsFound", "No plugins are currently available"},
            {"PermissionNotFound", "Permission '{0}' doesn't exist"},
            {"PermissionsNotLoaded", "Unable to load permission files! Permissions will not work until resolved.\n => {0}"},
            {"PlayerLanguage", "Player language set to {0}"},
            {"PluginNotLoaded", "Plugin '{0}' not loaded."},
            {"PluginReloaded", "Reloaded plugin {0} v{1} by {2}"},
            {"PluginUnloaded", "Unloaded plugin {0} v{1} by {2}"},
            {"ShowGroups", "Groups: {0}"},
            {"ServerLanguage", "Server language set to {0}"},
            {"UnknownCommand", "Unknown command: {0}"},
            {"UserAddedToGroup", "User '{0}' added to group: {1}"},
            {"UserNotFound", "User '{0}' not found"},
            {"UserPermissionGranted", "User '{0}' granted permission '{1}'"},
            {"UserPermissionRevoked", "User '{0}' revoked permission '{1}'"},
            {"UserRemovedFromGroup", "User '{0}' removed from group '{1}'"},
            {"YouAreNotAdmin", "You are not an admin"}
        };

        #endregion

        #region Core Hooks

        /// <summary>
        /// Called when the plugin is initializing
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote error logging
            RemoteLogger.SetTag("game", Title.ToLower());
            RemoteLogger.SetTag("game version", Server.Version);

            // Add core general commands
            AddCovalenceCommand(new[] { "oxide.lang", "lang" }, "LangCommand");
            AddCovalenceCommand(new[] { "oxide.version", "version" }, "VersionCommand");

            // Add core plugin commands
            AddCovalenceCommand(new[] { "oxide.plugins", "plugins" }, "PluginsCommand");
            AddCovalenceCommand(new[] { "oxide.load", "load" }, "LoadCommand");
            AddCovalenceCommand(new[] { "oxide.reload", "reload" }, "ReloadCommand");
            AddCovalenceCommand(new[] { "oxide.unload", "unload" }, "UnloadCommand");

            // Add core permission commands
            AddCovalenceCommand(new[] { "oxide.grant", "grant" }, "GrantCommand");
            AddCovalenceCommand(new[] { "oxide.group", "group" }, "GroupCommand");
            AddCovalenceCommand(new[] { "oxide.revoke", "revoke" }, "RevokeCommand");
            AddCovalenceCommand(new[] { "oxide.show", "show" }, "ShowCommand");
            AddCovalenceCommand(new[] { "oxide.usergroup", "usergroup" }, "UserGroupCommand");

            // Register core permissions
            permission.RegisterPermission("oxide.plugins", this);
            permission.RegisterPermission("oxide.load", this);
            permission.RegisterPermission("oxide.reload", this);
            permission.RegisterPermission("oxide.unload", this);
            permission.RegisterPermission("oxide.grant", this);
            permission.RegisterPermission("oxide.group", this);
            permission.RegisterPermission("oxide.revoke", this);
            permission.RegisterPermission("oxide.show", this);
            permission.RegisterPermission("oxide.usergroup", this);

            // Register messages for localization
            //lang.RegisterMessages(messages, this); // TODO: Implement

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
            SevenDaysExtension.ServerConsole();

            serverInitialized = true;
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion

        #region Helpers

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
    }
}
