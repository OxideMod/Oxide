using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using CodeHatch.Build;
using CodeHatch.Common;
using CodeHatch.Engine.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.ReignOfKings.Libraries;
using Oxide.Game.ReignOfKings.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings
{
    /// <summary>
    /// The core Reign of Kings plugin
    /// </summary>
    public class ReignOfKingsCore : CSPlugin
    {
        #region Initialization

        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();

        // The RoK permission library
        private CodeHatch.Permissions.Permission rokPerms;

        // The command library
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The Reign of Kings covalence provider
        private readonly ReignOfKingsCovalenceProvider covalence = ReignOfKingsCovalenceProvider.Instance;

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        // Track 'load' chat commands
        private readonly Dictionary<string, Player> loadingPlugins = new Dictionary<string, Player>();

        private static readonly FieldInfo FoldersField = typeof (FileCounter).GetField("_folders", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Initializes a new instance of the ReignOfKingsCore class
        /// </summary>
        public ReignOfKingsCore()
        {
            // Set attributes
            Name = "ReignOfKingsCore";
            Title = "Reign of Kings Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);

            var plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>();
            if (plugins.Exists("unitycore")) InitializeLogging();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(Player player)
        {
            if (permission.IsLoaded || player.IsServer) return true;
            ReplyWith(player, "Unable to load permission files! Permissions will not work until resolved.\n => " + permission.LastException.Message);
            return false;
        }

        #endregion

        #region Plugin Hooks

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote logging
            RemoteLogger.SetTag("game", "reign of kings");
            RemoteLogger.SetTag("version", GameInfo.VersionName.ToLower());

            // Add general commands
            cmdlib.AddChatCommand("oxide.plugins", this, "CmdPlugins");
            cmdlib.AddChatCommand("plugins", this, "CmdPlugins");
            cmdlib.AddChatCommand("oxide.load", this, "CmdLoad");
            cmdlib.AddChatCommand("load", this, "CmdLoad");
            cmdlib.AddChatCommand("oxide.unload", this, "CmdUnload");
            cmdlib.AddChatCommand("unload", this, "CmdUnload");
            cmdlib.AddChatCommand("oxide.reload", this, "CmdReload");
            cmdlib.AddChatCommand("reload", this, "CmdReload");
            cmdlib.AddChatCommand("oxide.version", this, "CmdVersion");
            cmdlib.AddChatCommand("version", this, "CmdVersion");

            // Add permission commands
            cmdlib.AddChatCommand("oxide.group", this, "CmdGroup");
            cmdlib.AddChatCommand("group", this, "CmdGroup");
            cmdlib.AddChatCommand("oxide.usergroup", this, "CmdUserGroup");
            cmdlib.AddChatCommand("usergroup", this, "CmdUserGroup");
            cmdlib.AddChatCommand("oxide.grant", this, "CmdGrant");
            cmdlib.AddChatCommand("grant", this, "CmdGrant");
            cmdlib.AddChatCommand("oxide.revoke", this, "CmdRevoke");
            cmdlib.AddChatCommand("revoke", this, "CmdRevoke");
            cmdlib.AddChatCommand("oxide.show", this, "CmdShow");
            cmdlib.AddChatCommand("show", this, "CmdShow");
        }

        /// <summary>
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore") InitializeLogging();

            if (!loadingPlugins.ContainsKey(plugin.Name)) return;
            ReplyWith(loadingPlugins[plugin.Name], $"Loaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
            loadingPlugins.Remove(plugin.Name);
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

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", DedicatedServerBypass.Settings.ServerName);

            // Load default permission groups
            rokPerms = Server.Permissions;
            if (permission.IsLoaded)
            {
                var rank = 0;
                var rokGroups = rokPerms.GetGroups();
                for (var i = rokGroups.Count - 1; i >= 0; i--)
                {
                    var defaultGroup = rokGroups[i].Name;
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
                permission.RegisterValidate(s =>
                {
                    ulong temp;
                    if (!ulong.TryParse(s, out temp))
                        return false;
                    var digits = temp == 0 ? 1 : (int)Math.Floor(Math.Log10(temp) + 1);
                    return digits >= 17;
                });
                permission.CleanUp();
            }
        }

        /// <summary>
        /// Called by the server when starting, wrapped to prevent errors with dynamic assemblies
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        [HookMethod("IGetTypeFromName")]
        private Type IGetTypeFromName(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly is System.Reflection.Emit.AssemblyBuilder) continue;
                try
                {
                    foreach (var type in assembly.GetExportedTypes())
                        if (type.Name == fullTypeName) return type;
                }
                catch
                {
                    // Ignored
                }
            }
            return null;
        }

        /// <summary>
        /// Called when the hash is recalculated
        /// </summary>
        /// <param name="fileHasher"></param>
        [HookMethod("IOnRecalculateHash")]
        private void IOnRecalculateHash(FileHasher fileHasher)
        {
            if (fileHasher.FileLocationFromDataPath.Equals("/Managed/Assembly-CSharp.dll"))
                fileHasher.FileLocationFromDataPath = "/Managed/Assembly-CSharp_Original.dll";
        }

        /// <summary>
        /// Called when the files are counted
        /// </summary>
        /// <param name="fileCounter"></param>
        [HookMethod("IOnCountFolder")]
        private void IOnCountFolder(FileCounter fileCounter)
        {
            if (fileCounter.FolderLocationFromDataPath.Equals("/Managed/") && fileCounter.Folders.Length != 39)
            {
                var folders = (string[])FoldersField.GetValue(fileCounter);
                Array.Resize(ref folders, 39);
                FoldersField.SetValue(fileCounter, folders);
            }
            else if (fileCounter.FolderLocationFromDataPath.Equals("/../") && fileCounter.Folders.Length != 2)
            {
                var folders = (string[])FoldersField.GetValue(fileCounter);
                Array.Resize(ref folders, 2);
                FoldersField.SetValue(fileCounter, folders);
            }
        }

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(Player player)
        {
            // Call out and see if we should reject
            var canlogin = Interface.CallHook("CanClientLogin", player) ?? Interface.CallHook("CanUserLogin", player.Name, player.Id.ToString());
            if (canlogin != null && (!(canlogin is bool) || !(bool)canlogin))
            {
                player.ShowPopup("Disconnected", canlogin.ToString());
                player.Connection.Close();
                return ConnectionError.NoError;
            }

            return Interface.CallHook("OnUserApprove", player) ?? Interface.CallHook("OnUserApproved", player.Name, player.Id.ToString());
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="message"></param>
        [HookMethod("OnPlayerChat")]
        private object OnPlayerChat(PlayerMessageEvent evt, string message)
        {
            // Call covalence hook
            var iplayer = covalence.PlayerManager.GetPlayer(evt.PlayerId.ToString());
            return Interface.CallHook("OnUserChat", iplayer, message);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(Player player)
        {
            // Let covalence know
            covalence.PlayerManager.NotifyPlayerConnect(player);

            // Call covalence hook
            var iplayer = covalence.PlayerManager.GetPlayer(player.Id.ToString());
            Interface.CallHook("OnUserConnected", iplayer);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(Player player)
        {
            // Call covalence hook
            var iplayer = covalence.PlayerManager.GetPlayer(player.Id.ToString());
            Interface.CallHook("OnUserDisconnected", iplayer);

            // Let covalence know
            covalence.PlayerManager.NotifyPlayerDisconnect(player);
        }

        /// <summary>
        /// Called when the player has been initialized and spawned into the game
        /// </summary>
        /// <param name="e"></param>
        [HookMethod("OnPlayerSpawn")]
        private void OnPlayerSpawn(PlayerFirstSpawnEvent e)
        {
            // Do permission stuff
            if (permission.IsLoaded)
            {
                var userId = e.Player.Id.ToString();
                permission.UpdateNickname(userId, e.Player.Name);

                // Add player to default group
                if (permission.GroupExists("default")) permission.AddUserGroup(userId, "default");
                else if (permission.GroupExists("guest")) permission.AddUserGroup(userId, "guest");
            }
        }

        #endregion

        #region Chat/Console Commands

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdPlugins")]
        private void CmdPlugins(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;

            var loaded_plugins = pluginmanager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
            var loaded_plugin_names = new HashSet<string>(loaded_plugins.Select(pl => pl.Name));
            var unloaded_plugin_errors = new Dictionary<string, string>();
            foreach (var loader in Interface.Oxide.GetPluginLoaders())
            {
                foreach (var name in loader.ScanDirectory(Interface.Oxide.PluginDirectory).Except(loaded_plugin_names))
                {
                    string msg;
                    unloaded_plugin_errors[name] = (loader.PluginErrors.TryGetValue(name, out msg)) ? msg : "Unloaded";
                }
            }

            var total_plugin_count = loaded_plugins.Length + unloaded_plugin_errors.Count;
            if (total_plugin_count < 1)
            {
                ReplyWith(player, "No plugins are currently available");
                return;
            }

            var output = $"Listing {loaded_plugins.Length + unloaded_plugin_errors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loaded_plugins) output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author}";
            foreach (var plugin_name in unloaded_plugin_errors.Keys) output += $"\n  {number++:00} {plugin_name} - {unloaded_plugin_errors[plugin_name]}";
            ReplyWith(player, output);
        }

        /// <summary>
        /// Called when the "load" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdLoad")]
        private void CmdLoad(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 1)
            {
                ReplyWith(player, "Usage: load *|<pluginname>+");
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name) || !Interface.Oxide.LoadPlugin(name)) continue;
                if (!loadingPlugins.ContainsKey(name)) loadingPlugins.Add(name, player);
            }
        }

        /// <summary>
        /// Called when the "reload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdReload")]
        private void CmdReload(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 1)
            {
                ReplyWith(player, "Usage: reload *|<pluginname>+");
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                // Reload
                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    ReplyWith(player, $"Plugin '{name}' not loaded.");
                    continue;
                }
                Interface.Oxide.ReloadPlugin(name);
                ReplyWith(player, $"Reloaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
            }
        }

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdUnload")]
        private void CmdUnload(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 1)
            {
                ReplyWith(player, "Usage: unload *|<pluginname>+");
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.UnloadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                // Unload
                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    ReplyWith(player, $"Plugin '{name}' not loaded.");
                    continue;
                }
                Interface.Oxide.UnloadPlugin(name);
                ReplyWith(player, $"Unloaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
            }
        }

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdVersion")]
        private void CmdVersion(Player player, string command, string[] args)
        {
            var oxide = OxideMod.Version.ToString();
            var game = GameInfo.VersionName;

            if (!string.IsNullOrEmpty(oxide) && !string.IsNullOrEmpty(game))
                ReplyWith(player, $"Oxide version: {oxide}, Reign of Kings version: {game}");
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdGroup")]
        private void CmdGroup(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 2)
            {
                ReplyWith(player, "Usage: group <add|remove|set> <name> [title] [rank]");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var title = args.Length > 2 ? args[2] : string.Empty;
            int rank;
            if (args.Length < 4 || !int.TryParse(args[3], out rank))
                rank = 0;

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(name))
                {
                    ReplyWith(player, "Group '" + name + "' already exist");
                    return;
                }
                permission.CreateGroup(name, title, rank);
                ReplyWith(player, "Group '" + name + "' created");
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RemoveGroup(name);
                ReplyWith(player, "Group '" + name + "' deleted");
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                ReplyWith(player, "Group '" + name + "' changed");
            }
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdUserGroup")]
        private void CmdUserGroup(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 3)
            {
                ReplyWith(player, "Usage: usergroup <add|remove> <username> <groupname>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = FindPlayer(name);
            if (target == null && !permission.UserIdValid(name))
            {
                ReplyWith(player, "User '" + name + "' not found");
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.Id.ToString();
                name = target.Name;
                permission.UpdateNickname(userId, name);
            }

            if (!permission.GroupExists(group))
            {
                ReplyWith(player, "Group '" + group + "' doesn't exist");
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                ReplyWith(player, "User '" + name + "' assigned group: " + group);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                ReplyWith(player, "User '" + name + "' removed from group: " + group);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdGrant")]
        private void CmdGrant(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 3)
            {
                ReplyWith(player, "Usage: grant <group|user> <name|id> <permission>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                ReplyWith(player, "Group '" + name + "' granted permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var target = FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    ReplyWith(player, "User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.Id.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.GrantUserPermission(userId, perm, null);
                ReplyWith(player, "User '" + name + "' granted permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdRevoke")]
        private void CmdRevoke(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 3)
            {
                ReplyWith(player, "Usage: revoke <group|user> <name|id> <permission>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    ReplyWith(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                ReplyWith(player, "Group '" + name + "' revoked permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var target = FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    ReplyWith(player, "User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.Id.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.RevokeUserPermission(userId, perm);
                ReplyWith(player, "User '" + name + "' revoked permission: " + perm);
            }
        }

        #endregion

        #region Show Command

        /// <summary>
        /// Called when the "show" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("CmdShow")]
        private void CmdShow(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;
            if (!HasPermission(player, "admin")) return;
            if (args.Length < 2)
            {
                ReplyWith(player, "Usage: show <group|user> <name>\nUsage: show <groups|perms>");
                return;
            }

            var mode = args[0];
            var name = args[1];

            if (mode.Equals("perms"))
            {
                ReplyWith(player, "Permissions:\n" + string.Join(", ", permission.GetPermissions()));
            }
            else if (mode.Equals("perm"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    ReplyWith(player, "Usage: show <group|user> <name>\nUsage: show <groups|perms>");
                    return;
                }

                var result = $"Permission '{name}' Users:\n";
                result += string.Join(", ", permission.GetPermissionUsers(name));
                result += $"\nPermission '{name}' Groups:\n";
                result += string.Join(", ", permission.GetPermissionGroups(name));
                ReplyWith(player, result);
            }
            else if (mode.Equals("user"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    ReplyWith(player, "Usage: show <group|user> <name>\nUsage: show <groups|perms>");
                    return;
                }

                var target = FindPlayer(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    ReplyWith(player, "User '" + name + "' not found");
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.Id.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }
                var result = $"User '{name}' permissions:\n";
                result += string.Join(", ", permission.GetUserPermissions(userId));
                result += $"\nUser '{name}' groups:\n";
                result += string.Join(", ", permission.GetUserGroups(userId));
                ReplyWith(player, result);
            }
            else if (mode.Equals("group"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    ReplyWith(player, "Usage: show <group|user> <name>\nUsage: show <groups|perms>");
                    return;
                }

                if (!permission.GroupExists(name) && !string.IsNullOrEmpty(name))
                {
                    ReplyWith(player, "Group '" + name + "' not found");
                    return;
                }

                var result = $"Group '{name}' users:\n";
                result += string.Join(", ", permission.GetUsersInGroup(name));
                result += $"\nGroup '{name}' permissions:\n";
                result += string.Join(", ", permission.GetGroupPermissions(name));
                var parent = permission.GetGroupParent(name);
                while (permission.GroupExists(parent))
                {
                    result += $"\nParent group '{parent}' permissions:\n";
                    result += string.Join(", ", permission.GetGroupPermissions(parent));
                    parent = permission.GetGroupParent(parent);
                }
                ReplyWith(player, result);
            }
            else if (mode.Equals("groups"))
            {
                ReplyWith(player, "Groups:\n" + string.Join(", ", permission.GetGroups()));
            }
        }

        #endregion

        #region Command Handling

        /// <summary>
        /// Called when a chat command was run
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerCommand")]
        private object IOnPlayerCommand(PlayerCommandEvent e)
        {
            if (e?.Player == null || e.Command == null) return null;

            var str = e.Command;
            if (str.Length == 0) return null;
            if (str[0] != '/') return null;

            // Is this a covalence command?
            var livePlayer = covalence.PlayerManager.GetOnlinePlayer(e.PlayerId.ToString());
            if (covalence.CommandSystem.HandleChatMessage(livePlayer, str)) return true;

            // Get the command string
            var command = str.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(command, out cmd, out args);
            if (cmd == null) return null;

            // Handle it
            if (!cmdlib.HandleChatCommand(e.Player, cmd, args)) return null;

            Interface.CallDeprecatedHook("OnPlayerCommand", "OnChatCommand", new DateTime(2016, 5, 19), e.Player, cmd, args);
            Interface.CallHook("OnChatCommand", e.Player, cmd, args);

            // Handled
            return true;
        }

        /// <summary>
        /// Parses the specified chat command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseChatCommand(string argstr, out string cmd, out string[] args)
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

        #endregion

        #region Helpers

        /// <summary>
        /// Replies to the player with a specific message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        private static void ReplyWith(Player player, string message) => player.SendMessage($"[950415]Server[FFFFFF]: {message}");

        /// <summary>
        /// Lookup the player using name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        private Player FindPlayer(string nameOrIdOrIp)
        {
            var player = Server.GetPlayerByName(nameOrIdOrIp);
            if (player == null)
            {
                ulong id;
                if (ulong.TryParse(nameOrIdOrIp, out id)) player = Server.GetPlayerById(id);
            }
            if (player == null)
            {
                foreach (var target in Server.ClientPlayers)
                    if (target.Connection.IpAddress == nameOrIdOrIp) player = target;
            }
            return player;
        }

        /// <summary>
        /// Checks if the player has the required permission
        /// </summary>
        /// <param name="player"></param>
        /// <param name="perm"></param>
        /// <returns></returns>
        private bool HasPermission(Player player, string perm)
        {
            if (serverInitialized && rokPerms.HasPermission(player.Name, perm) || permission.UserHasGroup(player.Id.ToString(), perm) || player.IsServer) return true;
            ReplyWith(player, "You don't have permission to use this command.");
            return false;
        }

        #endregion
    }
}
