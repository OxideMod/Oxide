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

using UnityEngine;

using Network = uLink.Network;
using RoKPermissions = CodeHatch.Permissions;

namespace Oxide.Game.ReignOfKings
{
    /// <summary>
    /// The core Reign of Kings plugin
    /// </summary>
    public class ReignOfKingsCore : CSPlugin
    {
        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission lib
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();

        // The RoK permission lib
        private RoKPermissions.Permission RoKPerms;

        // The command lib
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        // Track oxide.load chat commands
        private Dictionary<string, Player> loadingPlugins = new Dictionary<string, Player>();

        private static readonly FieldInfo FoldersField = typeof (FileCounter).GetField("_folders", BindingFlags.Instance | BindingFlags.NonPublic);

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
            var isServer = Network.isServer;
        }

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Add our commands
            cmdlib.AddChatCommand("oxide.plugins", this, "cmdPlugins");
            cmdlib.AddChatCommand("plugins", this, "cmdPlugins");
            cmdlib.AddChatCommand("oxide.load", this, "cmdLoad");
            cmdlib.AddChatCommand("load", this, "cmdLoad");
            cmdlib.AddChatCommand("oxide.unload", this, "cmdUnload");
            cmdlib.AddChatCommand("unload", this, "cmdUnload");
            cmdlib.AddChatCommand("oxide.reload", this, "cmdReload");
            cmdlib.AddChatCommand("reload", this, "cmdReload");
            cmdlib.AddChatCommand("oxide.version", this, "cmdVersion");
            cmdlib.AddChatCommand("version", this, "cmdVersion");

            cmdlib.AddChatCommand("oxide.group", this, "cmdGroup");
            cmdlib.AddChatCommand("group", this, "cmdGroup");
            cmdlib.AddChatCommand("oxide.usergroup", this, "cmdUserGroup");
            cmdlib.AddChatCommand("usergroup", this, "cmdUserGroup");
            cmdlib.AddChatCommand("oxide.grant", this, "cmdGrant");
            cmdlib.AddChatCommand("grant", this, "cmdGrant");
            cmdlib.AddChatCommand("oxide.revoke", this, "cmdRevoke");
            cmdlib.AddChatCommand("revoke", this, "cmdRevoke");

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
            if (serverInitialized) return;
            serverInitialized = true;
            RoKPerms = Server.Permissions;

            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", DedicatedServerBypass.Settings.ServerName);

            // Load default permission groups
            if (permission.IsLoaded)
            {
                var rank = 0;
                var RoKGroups = RoKPerms.GetGroups();
                for (var i = RoKGroups.Count - 1; i >= 0; i--)
                {
                    var defaultGroup = RoKGroups[i].Name;
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
            }
        }
        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(Player player)
        {
            if (permission.IsLoaded || player.IsServer) return true;
            SendPlayerMessage(player, "Unable to load permission files! Permissions will not work until the error has been resolved.\n => " + permission.LastException.Message);
            return false;
        }

        /// <summary>
        /// Sends a message to a specific player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        private static void SendPlayerMessage(Player player, string format, params object[] args)
        {
            if (player.IsServer)
                Interface.Oxide.LogInfo(format, args);
            else
                player.SendMessage("[950415]Oxide[FFFFFF]: " + format, args);
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
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore")
                InitializeLogging();
            if (!loadingPlugins.ContainsKey(plugin.Name)) return;
            SendPlayerMessage(loadingPlugins[plugin.Name], "Loaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            loadingPlugins.Remove(plugin.Name);
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
        /// Called when the "oxide.plugins" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdPlugins")]
        private void cmdPlugins(Player player, string command, string[] args)
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
                SendPlayerMessage(player, "No plugins are currently available");
                return;
            }

            var output = $"Listing {loaded_plugins.Length + unloaded_plugin_errors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loaded_plugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author}";
            foreach (var plugin_name in unloaded_plugin_errors.Keys)
                output += $"\n  {number++:00} {plugin_name} - {unloaded_plugin_errors[plugin_name]}";
            SendPlayerMessage(player, output);
        }

        /// <summary>
        /// Called when the "oxide.load" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdLoad")]
        private void cmdLoad(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;

            if (!HasPermission(player, "admin")) return;

            // Check arg 1 exists
            if (args.Length < 1)
            {
                SendPlayerMessage(player, "Syntax: oxide.load *|<pluginname>+");
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
        /// Called when the "oxide.unload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdUnload")]
        private void cmdUnload(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;

            if (!HasPermission(player, "admin")) return;

            // Check arg 1 exists
            if (args.Length < 1)
            {
                SendPlayerMessage(player, "Syntax: oxide.unload *|<pluginname>+");
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
                    SendPlayerMessage(player, "Plugin '{0}' not loaded.", name);
                    continue;
                }
                Interface.Oxide.UnloadPlugin(name);
                SendPlayerMessage(player, "Unloaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            }
        }

        /// <summary>
        /// Called when the "oxide.reload" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdReload")]
        private void cmdReload(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;

            if (!HasPermission(player, "admin")) return;

            // Check arg 1 exists
            if (args.Length < 1)
            {
                SendPlayerMessage(player, "Syntax: oxide.reload *|<pluginname>+");
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
                    SendPlayerMessage(player, "Plugin '{0}' not loaded.", name);
                    continue;
                }
                Interface.Oxide.ReloadPlugin(name);
                SendPlayerMessage(player, "Reloaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            }
        }

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdVersion")]
        private void cmdVersion(Player player, string command, string[] args)
        {
            // Get the Rust network protocol version at runtime
            var protocol = GameInfo.VersionName;

            // Get the Oxide Core version
            var oxide = OxideMod.Version.ToString();

            // Show the versions
            if (!string.IsNullOrEmpty(protocol) && !string.IsNullOrEmpty(oxide))
            {
                SendPlayerMessage(player, "Oxide Version: " + oxide + ", Reign of Kings version: " + protocol);
            }
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdGroup")]
        private void cmdGroup(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;

            if (!HasPermission(player, "admin")) return;

            // Check 2 args exists
            if (args.Length < 2)
            {
                SendPlayerMessage(player, "Syntax: oxide.group <add|remove|set> <name> [title] [rank]");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var title = args[2];
            int rank;
            if (!int.TryParse(args[3], out rank))
                rank = 0;

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(name))
                {
                    SendPlayerMessage(player, "Group '" + name + "' already exist");
                    return;
                }
                permission.CreateGroup(name, title, rank);
                SendPlayerMessage(player, "Group '" + name + "' created");
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    SendPlayerMessage(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RemoveGroup(name);
                SendPlayerMessage(player, "Group '" + name + "' deleted");
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    SendPlayerMessage(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                SendPlayerMessage(player, "Group '" + name + "' changed");
            }
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdUserGroup")]
        private void cmdUserGroup(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;

            if (!HasPermission(player, "admin")) return;

            // Check 3 args exists
            if (args.Length < 3)
            {
                SendPlayerMessage(player, "Syntax: oxide.usergroup <add|remove> <username> <groupname>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = FindPlayer(name);
            if (target == null && !permission.UserExists(name))
            {
                SendPlayerMessage(player, "User '" + name + "' not found");
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.Id.ToString();
                name = target.Name;
                permission.GetUserData(userId).LastSeenNickname = name;
            }

            if (!permission.GroupExists(group))
            {
                SendPlayerMessage(player, "Group '" + group + "' doesn't exist");
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                SendPlayerMessage(player, "User '" + name + "' assigned group: " + group);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                SendPlayerMessage(player, "User '" + name + "' removed from group: " + group);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdGrant")]
        private void cmdGrant(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;

            if (!HasPermission(player, "admin")) return;

            // Check 3 args exists
            if (args.Length < 3)
            {
                SendPlayerMessage(player, "Syntax: oxide.grant <group|user> <name|id> <permission>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    SendPlayerMessage(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                SendPlayerMessage(player, "Group '" + name + "' granted permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var target = FindPlayer(name);
                if (target == null && !permission.UserExists(name))
                {
                    SendPlayerMessage(player, "User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.Id.ToString();
                    name = target.Name;
                    permission.GetUserData(name).LastSeenNickname = name;
                }
                permission.GrantUserPermission(userId, perm, null);
                SendPlayerMessage(player, "User '" + name + "' granted permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("cmdRevoke")]
        private void cmdRevoke(Player player, string command, string[] args)
        {
            if (!PermissionsLoaded(player)) return;

            if (!HasPermission(player, "admin")) return;

            // Check 3 args exists
            if (args.Length < 3)
            {
                SendPlayerMessage(player, "Syntax: oxide.revoke <group|user> <name|id> <permission>");
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    SendPlayerMessage(player, "Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                SendPlayerMessage(player, "Group '" + name + "' revoked permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var target = FindPlayer(name);
                if (target == null && !permission.UserExists(name))
                {
                    SendPlayerMessage(player, "User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.Id.ToString();
                    name = target.Name;
                    permission.GetUserData(name).LastSeenNickname = name;
                }
                permission.RevokeUserPermission(userId, perm);
                SendPlayerMessage(player, "User '" + name + "' revoked permission: " + perm);
            }
        }

        /// <summary>
        /// Looks for a player
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        private Player FindPlayer(string nameOrIdOrIp)
        {
            var player = Server.GetPlayerByName(nameOrIdOrIp);
            if (player == null)
            {
                ulong id;
                if (ulong.TryParse(nameOrIdOrIp, out id))
                    player = Server.GetPlayerById(id);
            }
            if (player == null)
            {
                foreach (var target in Server.ClientPlayers.Where(target => target.Connection.IpAddress == nameOrIdOrIp))
                    player = target;
            }
            return player;
        }

        /// <summary>
        /// Checks if a player has the required permission
        /// </summary>
        /// <param name="player"></param>
        /// <param name="perm"></param>
        /// <returns></returns>
        private bool HasPermission(Player player, string perm)
        {
            if (serverInitialized && RoKPerms.HasPermission(player.Name, perm) || permission.UserHasGroup(player.Id.ToString(), perm) || player.IsServer) return true;
            SendPlayerMessage(player, "You don't have permission to use this command.");
            return false;
        }

        /// <summary>
        /// Called by the server when starting, wrapped to prevent errors with dynamic assemblies.
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        [HookMethod("IGetTypeFromName")]
        private Type IGetTypeFromName(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly is System.Reflection.Emit.AssemblyBuilder) continue;
                foreach (var type in assembly.GetExportedTypes())
                    if (type.Name == fullTypeName)
                        return type;
            }
            return null;
        }

        /// <summary>
        /// Called when a player connects to the server
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerConnected")]
        private object IOnPlayerConnected(Player player)
        {
            if (player.Id == 9999999999) return null;

            return Interface.CallHook("OnPlayerConnected", player);
        }

        /// <summary>
        /// Called when a chat message was sent
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(PlayerEvent e)
        {
            if (e.SenderId == 9999999999) return null;

            return Interface.CallHook("OnPlayerChat", e);
        }

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

            // Get the message
            var message = str.Substring(1);

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

        /// <summary>
        /// Called when the player has been initialized and spawned into the game
        /// </summary>
        /// <param name="e"></param>
        [HookMethod("OnPlayerSpawn")]
        void OnPlayerSpawn(PlayerFirstSpawnEvent e)
        {
            if (!permission.IsLoaded) return;
            var userId = e.Player.Id.ToString();
            permission.GetUserData(userId).LastSeenNickname = e.Player.Name;

            // Add player to default group
            if (permission.GroupExists("default"))
                permission.AddUserGroup(userId, "default");
            else if (permission.GroupExists("guest"))
                permission.AddUserGroup(userId, "guest");
        }

        /// <summary>
        /// Called when the hash is recalculated
        /// </summary>
        /// <param name="fileHasher"></param>
        [HookMethod("OnRecalculateHash")]
        void OnRecalculateHash(FileHasher fileHasher)
        {
            if (fileHasher.FileLocationFromDataPath.Equals("/Managed/Assembly-CSharp.dll"))
                fileHasher.FileLocationFromDataPath = "/Managed/Assembly-CSharp_Original.dll";
        }

        /// <summary>
        /// Called when the files are counted
        /// </summary>
        /// <param name="fileCounter"></param>
        [HookMethod("OnCountFolder")]
        void OnCountFolder(FileCounter fileCounter)
        {
            if (fileCounter.FolderLocationFromDataPath.Equals("/Managed/") && fileCounter.Folders.Length != 39)
            {
                var folders = (string[]) FoldersField.GetValue(fileCounter);
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

        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(Player player)
        {
            Libraries.Covalence.ReignOfKingsCovalenceProvider.Instance.PlayerManager.NotifyPlayerDisconnect(player);
        }

        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(Player player)
        {
            Libraries.Covalence.ReignOfKingsCovalenceProvider.Instance.PlayerManager.NotifyPlayerConnect(player);
        }
    }
}
