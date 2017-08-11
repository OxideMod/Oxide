﻿﻿﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.ReignOfKings.Libraries;
using Oxide.Game.ReignOfKings.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings
{
    /// <summary>
    /// The core Reign of Kings plugin
    /// </summary>
    public partial class ReignOfKingsCore : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the ReignOfKingsCore class
        /// </summary>
        public ReignOfKingsCore()
        {
            // Set plugin info attributes
            Title = "Reign of Kings";
            Author = "Oxide Team";
            var assemblyVersion = ReignOfKingsExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);

            CommandManager.OnRegisterCommand += (attribute) =>
            {
                foreach (var command in attribute.Aliases.InsertItem(attribute.Name, 0))
                {
                    Command.ChatCommand chatCommand;
                    if (cmdlib.ChatCommands.TryGetValue(command, out chatCommand))
                    {
                        cmdlib.ChatCommands.Remove(chatCommand.Name);
                        cmdlib.AddChatCommand(chatCommand.Name, chatCommand.Plugin, chatCommand.Callback);
                    }

                    ReignOfKingsCommandSystem.RegisteredCommand covalenceCommand;
                    if (Covalence.CommandSystem.registeredCommands.TryGetValue(command, out covalenceCommand))
                    {
                        Covalence.CommandSystem.registeredCommands.Remove(covalenceCommand.Command);
                        Covalence.CommandSystem.RegisterCommand(covalenceCommand.Command, covalenceCommand.Source, covalenceCommand.Callback);
                    }
                }
            };
        }

        // Libraries
        internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        //internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly ReignOfKingsCovalenceProvider Covalence = ReignOfKingsCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = Covalence.CreateServer();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        // The RoK permission library
        private CodeHatch.Permissions.Permission rokPerms;

        private bool serverInitialized;

        // Track 'load' chat commands
        private readonly Dictionary<string, Player> loadingPlugins = new Dictionary<string, Player>();

        private static readonly FieldInfo FoldersField = typeof(FileCounter).GetField("_folders", BindingFlags.Instance | BindingFlags.NonPublic);

        #endregion

        #region Localization

        private readonly Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"CommandUsageGrant", "Usage: grant <group|user> <name|id> <permission>"},
            {"CommandUsageGroup", "Usage: group <add|set> <name> [title] [rank]"},
            {"CommandUsageGroupParent", "Usage: group <parent> <name> <parentName>"},
            {"CommandUsageGroupRemove", "Usage: group <remove> <name>"},
            {"CommandUsageLang", "Usage: lang <two-digit language code>"},
            {"CommandUsageLoad", "Usage: load *|<pluginname>+"},
            {"CommandUsageReload", "Usage: reload *|<pluginname>+"},
            {"CommandUsageRevoke", "Usage: revoke <group|user> <name|id> <permission>"},
            {"CommandUsageShow", "Usage: show <group|user> <name>\nUsage: show <groups|perms>"}, // TODO: Split this up
            {"CommandUsageUnload", "Usage: unload *|<pluginname>+"},
            {"CommandUsageUserGroup", "Usage: usergroup <add|remove> <username> <groupname>"},
            {"ConnectionRejected", "Connection was rejected"},
            {"GroupAlreadyExists", "Group '{0}' already exists"},
            {"GroupAlreadyHasPermission", "Group '{0}' already has permission '{1}'"},
            {"GroupDoesNotHavePermission", "Group '{0}' does not have permission '{1}'"},
            {"GroupChanged", "Group '{0}' changed"},
            {"GroupCreated", "Group '{0}' created"},
            {"GroupDeleted", "Group '{0}' deleted"},
            {"GroupNotFound", "Group '{0}' doesn't exist"},
            {"GroupParentChanged", "Group '{0}' parent changed to '{1}'"},
            {"GroupParentNotChanged", "Group '{0}' parent was not changed"},
            {"GroupParentNotFound", "Group parent '{0}' doesn't exist"},
            {"GroupPermissionGranted", "Group '{0}' granted permission '{1}'"},
            {"GroupPermissionRevoked", "Group '{0}' revoked permission '{1}'"},
            {"GroupPermissions", "Group '{0}' permissions"},
            {"GroupUsers", "Group '{0}' users"},
            {"Groups", "Groups"},
            {"NoGroupPermissions", "No permissions currently granted"},
            {"NoPermissionGroups", "No groups with this permission"},
            {"NoPermissionUsers", "No users with this permission"},
            {"NoPluginsFound", "No plugins are currently available"},
            {"NoUserGroups", "User is not assigned to any groups"},
            {"NoUserPermissions", "No permissions currently granted"},
            {"NoUsersInGroup", "No users currently in group"},
            {"NotAllowed", "You are not allowed to use the '{0}' command"},
            {"ParentGroupPermissions", "Parent group '{0}' permissions"},
            {"PermissionGroups", "Permission '{0}' Groups"},
            {"PermissionUsers", "Permission '{0}' Users"},
            {"PermissionNotFound", "Permission '{0}' doesn't exist"},
            {"Permissions", "Permissions"},
            {"PermissionsNotLoaded", "Unable to load permission files! Permissions will not work until resolved.\n => {0}"},
            {"PlayerLanguage", "Player language set to {0}"},
            {"PluginNotLoaded", "Plugin '{0}' not loaded."},
            {"PluginReloaded", "Reloaded plugin {0} v{1} by {2}"},
            {"PluginUnloaded", "Unloaded plugin {0} v{1} by {2}"},
            {"ServerLanguage", "Server language set to {0}"},
            {"UnknownCommand", "Unknown command: {0}"},
            {"UserAddedToGroup", "User '{0}' added to group: {1}"},
            {"UserAlreadyHasPermission", "User '{0}' already has permission '{1}'"},
            {"UserDoesNotHavePermission", "User '{0}' does not have permission '{1}'"},
            {"UserNotFound", "User '{0}' not found"},
            {"UserGroups", "User '{0}' groups"},
            {"UserPermissions", "User '{0}' permissions"},
            {"UserPermissionGranted", "User '{0}' granted permission '{1}'"},
            {"UserPermissionRevoked", "User '{0}' revoked permission '{1}'"},
            {"UserRemovedFromGroup", "User '{0}' removed from group '{1}'"},
            {"YouAreNotAdmin", "You are not an admin"}
        };

        #endregion

        #region Core Hooks

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
            lang.RegisterMessages(messages, this);
        }

        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            // Call OnServerInitialized for hotloaded plugins
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
        }

        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (serverInitialized) return;

            // Setup default permission groups
            rokPerms = CodeHatch.Engine.Networking.Server.Permissions;
            if (permission.IsLoaded)
            {
                var rank = 0;
                var rokGroups = rokPerms.GetGroups();
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

            Analytics.Collect();
            ReignOfKingsExtension.ServerConsole();

            serverInitialized = true;
        }

        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion

        #region Command Handling

        /// <summary>
        /// Parses the specified command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseCommand(string argstr, out string cmd, out string[] args)
        {
            var arglist = new List<string>();
            var sb = new StringBuilder();
            var inlongarg = false;
            foreach (var c in argstr)
            {
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        var arg = sb.ToString().Trim();
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
                    var arg = sb.ToString().Trim();
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
                var arg = sb.ToString().Trim();
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

        [HookMethod("IOnServerCommand")]
        private object IOnServerCommand(ulong id, string str)
        {
            if (str.Length == 0) return null;
            if (Interface.Call("OnServerCommand", str) != null) return true;

            // Check if command is from a player
            var player = CodeHatch.Engine.Networking.Server.GetPlayerById(id);
            if (player == null) return null;

            // Get the full command
            var message = str.TrimStart('/');

            // Parse it
            string cmd;
            string[] args;
            ParseCommand(message, out cmd, out args);
            if (cmd == null) return null;

            // Get the covalence player
            var iplayer = Covalence.PlayerManager.FindPlayerById(id.ToString());
            if (iplayer == null) return null;

            // Is the command blocked?
            var blockedSpecific = Interface.Call("OnPlayerCommand", player, cmd, args);
            var blockedCovalence = Interface.Call("OnUserCommand", iplayer, cmd, args);
            if (blockedSpecific != null || blockedCovalence != null) return true;

            // Is it a chat command?
            if (str[0] != '/') return null;

            // Is it a covalance command?
            if (Covalence.CommandSystem.HandleChatMessage(iplayer, str)) return true;

            // Is it a regular chat command?
            if (cmdlib.HandleChatCommand(player, cmd, args)) return true;

            return null;
        }

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
