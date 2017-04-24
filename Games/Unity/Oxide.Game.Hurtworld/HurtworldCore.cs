using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Hurtworld.Libraries;
using Oxide.Game.Hurtworld.Libraries.Covalence;
using Steamworks;
using UnityEngine;

namespace Oxide.Game.Hurtworld
{
    /// <summary>
    /// The core Hurtworld plugin
    /// </summary>
    public class HurtworldCore : CSPlugin
    {
        #region Initialization

        // The pluginmanager
        internal readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission library
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        internal static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // The command library
        internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // The player library
        internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // The covalence provider
        internal static readonly HurtworldCovalenceProvider Covalence = HurtworldCovalenceProvider.Instance;

        #region Localization

        // The language library
        private readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
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

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        // Track 'load' chat commands
        private readonly List<string> loadingPlugins = new List<string>();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            "bindip", "host", "queryport"
        };

        /// <summary>
        /// Initializes a new instance of the HurtworldCore class
        /// </summary>
        public HurtworldCore()
        {
            var assemblyVersion = HurtworldExtension.AssemblyVersion;

            Name = "HurtworldCore";
            Title = "Hurtworld";
            Author = "Oxide Team";
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);

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
        private bool PermissionsLoaded(PlayerSession session)
        {
            if (permission.IsLoaded) return true;
            Reply(Lang("PermissionsNotLoaded", permission.LastException.Message), session);
            return false;
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
            RemoteLogger.SetTag("game version", GameManager.Instance.Version.ToString());

            // Register messages for localization
            lang.RegisterMessages(messages, this);

            // Add general commands
            //cmdlib.AddChatCommand("oxide.plugins", this, "ChatPlugins");
            //cmdlib.AddChatCommand("plugins", this, "ChatPlugins");
            cmdlib.AddConsoleCommand("oxide.plugins", this, "ConsolePlugins");
            cmdlib.AddConsoleCommand("plugins", this, "ConsolePlugins");
            cmdlib.AddChatCommand("oxide.load", this, "ChatLoad");
            cmdlib.AddChatCommand("load", this, "ChatLoad");
            cmdlib.AddConsoleCommand("oxide.load", this, "ConsoleLoad");
            cmdlib.AddConsoleCommand("load", this, "ConsoleLoad");
            cmdlib.AddChatCommand("oxide.unload", this, "ChatUnload");
            cmdlib.AddChatCommand("unload", this, "ChatUnload");
            cmdlib.AddConsoleCommand("oxide.unload", this, "ConsoleUnload");
            cmdlib.AddConsoleCommand("unload", this, "ConsoleUnload");
            cmdlib.AddChatCommand("oxide.reload", this, "ChatReload");
            cmdlib.AddChatCommand("reload", this, "ChatReload");
            cmdlib.AddConsoleCommand("oxide.reload", this, "ConsoleReload");
            cmdlib.AddConsoleCommand("reload", this, "ConsoleReload");
            cmdlib.AddChatCommand("oxide.version", this, "ChatChatVersion");
            cmdlib.AddChatCommand("version", this, "ChatVersion");
            cmdlib.AddConsoleCommand("oxide.version", this, "ConsoleVersion");
            cmdlib.AddConsoleCommand("version", this, "ConsoleVersion");
            cmdlib.AddChatCommand("oxide.lang", this, "ChatLang");
            cmdlib.AddChatCommand("lang", this, "ChatLang");
            cmdlib.AddConsoleCommand("oxide.lang", this, "ConsoleLang");
            cmdlib.AddConsoleCommand("lang", this, "ConsoleLang");

            // Add permission commands
            cmdlib.AddChatCommand("oxide.group", this, "ChatGroup");
            cmdlib.AddChatCommand("group", this, "ChatGroup");
            cmdlib.AddConsoleCommand("oxide.group", this, "ConsoleGroup");
            cmdlib.AddConsoleCommand("group", this, "ConsoleGroup");
            cmdlib.AddChatCommand("oxide.usergroup", this, "ChatUserGroup");
            cmdlib.AddChatCommand("usergroup", this, "ChatUserGroup");
            cmdlib.AddConsoleCommand("oxide.usergroup", this, "ConsoleUserGroup");
            cmdlib.AddConsoleCommand("usergroup", this, "ConsoleUserGroup");
            cmdlib.AddChatCommand("oxide.grant", this, "ChatGrant");
            cmdlib.AddChatCommand("grant", this, "ChatGrant");
            cmdlib.AddConsoleCommand("oxide.grant", this, "ConsoleGrant");
            cmdlib.AddConsoleCommand("grant", this, "ConsoleGrant");
            cmdlib.AddChatCommand("oxide.revoke", this, "ChatRevoke");
            cmdlib.AddChatCommand("revoke", this, "ChatRevoke");
            cmdlib.AddConsoleCommand("oxide.revoke", this, "ConsoleRevoke");
            cmdlib.AddConsoleCommand("revoke", this, "ConsoleRevoke");
            cmdlib.AddChatCommand("oxide.show", this, "ChatShow");
            cmdlib.AddChatCommand("show", this, "ChatShow");
            cmdlib.AddConsoleCommand("oxide.show", this, "ConsoleShow");
            cmdlib.AddConsoleCommand("show", this, "ConsoleShow");

            // Setup the default permission groups
            if (permission.IsLoaded)
            {
                var rank = 0;
                for (var i = DefaultGroups.Length - 1; i >= 0; i--)
                {
                    var defaultGroup = DefaultGroups[i];
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
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
        /// Called when a plugin is loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (serverInitialized) plugin.CallHook("OnServerInitialized");
            if (!loggingInitialized && plugin.Name == "unitycore") InitializeLogging();

            if (!loadingPlugins.Contains(plugin.Name)) return;

            Interface.Oxide.LogInfo($"Loaded plugin {plugin.Title} v{plugin.Version} by {plugin.Author}");
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

            SteamGameServer.SetGameTags("oxide,modded");
            HurtworldExtension.ServerConsole();
            Analytics.Collect();
        }

        /// <summary>
        /// Called when the server is saving
        /// </summary>
        [HookMethod("OnServerSave")]
        private void OnServerSave() => Analytics.Collect();

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(PlayerSession session)
        {
            session.Name = session.Identity.Name ?? "Unnamed";
            var id = session.SteamId.ToString();
            var ip = session.Player.ipAddress;

            var loginSpecific = Interface.Call("CanClientLogin", session);
            var loginCovalence = Interface.Call("CanUserLogin", session.Name, id, ip);
            var canLogin = loginSpecific ?? loginCovalence;

            if (canLogin is string || (canLogin is bool && !(bool)canLogin))
            {
                GameManager.Instance.KickPlayer(id, canLogin is string ? canLogin.ToString() : "Connection was rejected"); // TODO: Localization
                return true;
            }

            var approvedSpecific = Interface.Call("OnUserApprove", session);
            var approvedCovalence = Interface.Call("OnUserApproved", session.Name, id, ip);
            return approvedSpecific ?? approvedCovalence;
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(PlayerSession session, string message)
        {
            if (message.Trim().Length <= 1) return true;
            var str = message.Substring(0, 1);

            // Get covalence player
            var iplayer = Covalence.PlayerManager.FindPlayerById(session.SteamId.ToString());

            // Is it a chat command?
            if (!str.Equals("/"))
            {
                var chatSpecific = Interface.Call("OnPlayerChat", session, message);
                var chatCovalence = iplayer != null ? Interface.Call("OnUserChat", iplayer, message) : null;
                return chatSpecific ?? chatCovalence;
            }

            // Is this a covalence command?
            if (Covalence.CommandSystem.HandleChatMessage(iplayer, message)) return true;

            // Get the command string
            var command = message.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(command, out cmd, out args);
            if (cmd == null) return null;

            // Handle it
            if (!cmdlib.HandleChatCommand(session, cmd, args))
            {
                Reply(Lang("UnknownCommand", session.SteamId.ToString(), cmd), session);
                return true;
            }

            // Call the game hook
            Interface.Call("OnChatCommand", session, command);

            return true;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="name"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(string name)
        {
            var session = Player.Find(name);
            if (session == null) return;

            // Do permission stuff
            if (permission.IsLoaded)
            {
                var id = session.SteamId.ToString();

                // Update stored name
                permission.UpdateNickname(id, session.Name);

                // Add player to default group
                if (!permission.UserHasGroup(id, DefaultGroups[0])) permission.AddUserGroup(id, DefaultGroups[0]);

                // Add player to admin group if admin
                if (session.IsAdmin && !permission.UserHasGroup(id, DefaultGroups[2])) permission.AddUserGroup(id, DefaultGroups[2]);
            }

            // Call game hook
            Interface.Call("OnPlayerConnected", session);

            // Let covalence know
            Covalence.PlayerManager.NotifyPlayerConnect(session);
            var iplayer = Covalence.PlayerManager.FindPlayerById(session.SteamId.ToString());
            if (iplayer != null) Interface.Call("OnUserConnected", iplayer);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="session"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(PlayerSession session)
        {
            // Let covalence know
            var iplayer = Covalence.PlayerManager.FindPlayerById(session.SteamId.ToString());
            if (iplayer != null) Interface.Call("OnUserDisconnected", iplayer, "Unknown");
            Covalence.PlayerManager.NotifyPlayerDisconnect(session);
        }

        /// <summary>
        /// Called when the server receives input from a player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="input"></param>
        [HookMethod("IOnPlayerInput")]
        private void IOnPlayerInput(uLink.NetworkPlayer player, InputControls input)
        {
            var session = Player.Find(player);
            if (session != null) Interface.Call("OnPlayerInput", session, input);
        }

        /// <summary>
        /// Called when the player attempts to suicide
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("IOnPlayerSuicide")]
        private object IOnPlayerSuicide(uLink.NetworkPlayer player)
        {
            var session = Player.Find(player);
            return session != null ? Interface.Call("OnPlayerSuicide", session) : null;
        }

        /// <summary>
        /// Called when the player attempts to suicide
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("IOnPlayerVoice")]
        private object IOnPlayerVoice(uLink.NetworkPlayer player)
        {
            var session = Player.Find(player);
            return session != null ? Interface.Call("OnPlayerVoice", session) : null;
        }

        #endregion

        #region Structure Hooks

        /// <summary>
        /// Called when a single door is used
        /// </summary>
        /// <param name="door"></param>
        /// <returns></returns>
        [HookMethod("IOnSingleDoorUsed")]
        private void IOnSingleDoorUsed(DoorSingleServer door)
        {
            var player = door.LastUsedBy;
            if (player == null) return;

            var session = Player.Find((uLink.NetworkPlayer)player);
            if (session != null) Interface.Call("OnSingleDoorUsed", door, session);
        }

        /// <summary>
        /// Called when a double door is used
        /// </summary>
        /// <param name="door"></param>
        /// <returns></returns>
        [HookMethod("IOnDoubleDoorUsed")]
        private void IOnDoubleDoorUsed(DoubleDoorServer door)
        {
            var player = door.LastUsedBy;
            if (player == null) return;

            var session = Player.Find((uLink.NetworkPlayer)player);
            if (session != null) Interface.Call("OnDoubleDoorUsed", door, session);
        }

        /// <summary>
        /// Called when a garage door is used
        /// </summary>
        /// <param name="door"></param>
        /// <returns></returns>
        [HookMethod("IOnGarageDoorUsed")]
        private void IOnGarageDoorUsed(GarageDoorServer door)
        {
            var player = door.LastUsedBy;
            if (player == null) return;

            var session = Player.Find((uLink.NetworkPlayer)player);
            if (session != null) Interface.Call("OnGarageDoorUsed", door, session);
        }

        #endregion

        #region Vehicle Hooks

        /// <summary>
        /// Called when a player tries to enter a vehicle
        /// </summary>
        /// <param name="session"></param>
        /// <param name="go"></param>
        /// <returns></returns>
        [HookMethod("ICanEnterVehicle")]
        private object ICanEnterVehicle(PlayerSession session, GameObject go)
        {
            return Interface.Call("CanEnterVehicle", session, go.GetComponent<VehiclePassenger>());
        }

        /// <summary>
        /// Called when a player tries to exit a vehicle
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        [HookMethod("ICanExitVehicle")]
        private object ICanExitVehicle(VehiclePassenger vehicle)
        {
            var session = Player.Find(vehicle.networkView.owner);
            return session != null ? Interface.Call("CanExitVehicle", session, vehicle) : null;
        }

        /// <summary>
        /// Called when a player enters a vehicle
        /// </summary>
        /// <param name="player"></param>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        [HookMethod("IOnEnterVehicle")]
        private void IOnEnterVehicle(uLink.NetworkPlayer player, VehiclePassenger vehicle)
        {
            var session = Player.Find(player);
            Interface.Call("OnEnterVehicle", session, vehicle);
        }

        /// <summary>
        /// Called when a player exits a vehicle
        /// </summary>
        /// <param name="player"></param>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        [HookMethod("IOnExitVehicle")]
        private void IOnExitVehicle(uLink.NetworkPlayer player, VehiclePassenger vehicle)
        {
            var session = Player.Find(player);
            Interface.Call("OnExitVehicle", session, vehicle);
        }

        #endregion

        #region Entity Hooks

        [HookMethod("IOnTakeDamage")]
        private void IOnTakeDamage(EntityEffectFluid effect, EntityStats target, EntityEffectSourceData source)
        {
            if (effect == null || target == null || source?.Value == 0) return;

            switch (effect.GetEffectType())
            {
                case EEntityFluidEffectType.PlayerToCreatureDamage:
                    var ent = target.GetComponent<AIEntity>();
                    if (ent != null)
                        Interface.CallHook("OnEntityTakeDamage", ent, source);
                    break;
                case EEntityFluidEffectType.CreatureToPlayerDamage:
                case EEntityFluidEffectType.Damage:
                case EEntityFluidEffectType.FallDamageProxy:
                    if (target.GetComponent<AIEntity>() != null) break;
                    var networkView = target.GetComponent<uLinkNetworkView>();
                    if (networkView == null) break;
                    var session = GameManager.Instance.GetSession(networkView.owner);
                    if (session != null)
                        Interface.CallHook("OnPlayerTakeDamage", session, source);
                    break;
            }
        }

        #endregion

        #region Chat/Console Commands

        #region Plugins Command

        /// <summary>
        /// Called when the "plugins" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatPlugins")]
        private void ChatPlugins(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;

            var loadedPlugins = pluginmanager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
            var loadedPluginNames = new HashSet<string>(loadedPlugins.Select(pl => pl.Name));
            var unloadedPluginErrors = new Dictionary<string, string>();
            foreach (var loader in Interface.Oxide.GetPluginLoaders())
            {
                foreach (var name in loader.ScanDirectory(Interface.Oxide.PluginDirectory).Except(loadedPluginNames))
                {
                    string msg;
                    unloadedPluginErrors[name] = (loader.PluginErrors.TryGetValue(name, out msg)) ? msg : "Unloaded";
                }
            }

            var totalPluginCount = loadedPlugins.Length + unloadedPluginErrors.Count;
            if (totalPluginCount < 1)
            {
                Reply(Lang("NoPluginsFound", session.SteamId.ToString()), session);
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loadedPlugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s)";
            foreach (var pluginName in unloadedPluginErrors.Keys)
                output += $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}";
            Reply(output, session);
        }

        /// <summary>
        /// Called when the "plugins" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsolePlugins")]
        private void ConsolePlugins(string command, string[] args)
        {
            var loadedPlugins = pluginmanager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();
            var loadedPluginNames = new HashSet<string>(loadedPlugins.Select(pl => pl.Name));
            var unloadedPluginErrors = new Dictionary<string, string>();
            foreach (var loader in Interface.Oxide.GetPluginLoaders())
            {
                foreach (var name in loader.ScanDirectory(Interface.Oxide.PluginDirectory).Except(loadedPluginNames))
                {
                    string msg;
                    unloadedPluginErrors[name] = (loader.PluginErrors.TryGetValue(name, out msg)) ? msg : "Unloaded";
                }
            }

            var totalPluginCount = loadedPlugins.Length + unloadedPluginErrors.Count;
            if (totalPluginCount < 1)
            {
                Reply(Lang("NoPluginsFound"));
                return;
            }

            var output = $"Listing {loadedPlugins.Length + unloadedPluginErrors.Count} plugins:";
            var number = 1;
            foreach (var plugin in loadedPlugins)
                output += $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author} ({plugin.TotalHookTime:0.00}s)";
            foreach (var pluginName in unloadedPluginErrors.Keys)
                output += $"\n  {number++:00} {pluginName} - {unloadedPluginErrors[pluginName]}";
            Reply(output);
        }

        #endregion

        #region Load Command

        /// <summary>
        /// Called when the "load" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatLoad")]
        private void ChatLoad(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageLoad", session.SteamId.ToString()), session);
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in args)
            {
                if (!string.IsNullOrEmpty(name) || !Interface.Oxide.LoadPlugin(name)) continue;
                if (!loadingPlugins.Contains(name)) loadingPlugins.Add(name);
            }
        }

        /// <summary>
        /// Called when the "load" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleLoad")]
        private void ConsoleLoad(string command, string[] args)
        {
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageLoad"));
                return;
            }

            if (args[0].Equals("*"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in args)
                if (!string.IsNullOrEmpty(name)) Interface.Oxide.LoadPlugin(name);
        }

        #endregion

        #region Reload Command

        /// <summary>
        /// Called when the "reload" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatReload")]
        private void ChatReload(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageReload", session.SteamId.ToString()), session);
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

                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    Reply(Lang("PluginNotLoaded", session.SteamId.ToString(), name), session);
                    continue;
                }

                Interface.Oxide.ReloadPlugin(name);
                Reply(Lang("PluginReloaded", session.SteamId.ToString(), plugin.Title, plugin.Version, plugin.Author), session);
            }
        }

        /// <summary>
        /// Called when the "reload" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleReload")]
        private void ConsoleReload(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Reply(Lang("CommandUsageReload"));
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

                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    Reply(Lang("PluginNotLoaded", null, name));
                    continue;
                }

                Interface.Oxide.ReloadPlugin(name);
            }
        }

        #endregion

        #region Unload Command

        /// <summary>
        /// Called when the "unload" command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatUnload")]
        private void ChatUnload(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageUnload", session.SteamId.ToString()), session);
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

                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    Reply(Lang("PluginNotLoaded", session.SteamId.ToString(), name), session);
                    continue;
                }

                Interface.Oxide.UnloadPlugin(name);
                Reply(Lang("PluginUnloaded", session.SteamId.ToString(), plugin.Title, plugin.Version, plugin.Author), session);
            }
        }

        /// <summary>
        /// Called when the "unload" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleUnload")]
        private void ConsoleUnload(string command, string[] args)
        {
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageUnload"));
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

                var plugin = pluginmanager.GetPlugin(name);
                if (plugin == null)
                {
                    Reply(Lang("PluginNotLoaded", null, name));
                    continue;
                }

                Interface.Oxide.UnloadPlugin(name);
            }
        }

        #endregion

        #region Version Command

        /// <summary>
        /// Called when the "version" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        [HookMethod("ChatVersion")]
        private void ChatVersion(PlayerSession session)
        {
            Reply($"Oxide {OxideMod.Version} for {Title} {GameManager.Instance.Version} ({GameManager.PROTOCOL_VERSION})", session);
        }

        /// <summary>
        /// Called when the "version" console command has been executed
        /// </summary>
        [HookMethod("ConsoleVersion")]
        private void ConsoleVersion(string command, string[] args) => ChatVersion(null);

        #endregion

        #region Lang Command

        /// <summary>
        /// Called when the "lang" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatLang")]
        private void ChatLang(PlayerSession session, string command, string[] args)
        {
            var id = session.SteamId.ToString();
            if (args != null && args.Length > 0) lang.SetLanguage(args[0], id);
            Reply(Lang("PlayerLanguage", id, lang.GetLanguage(id), session));
        }

        /// <summary>
        /// Called when the "lang" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleLang")]
        private void ConsoleLang(string command, string[] args)
        {
            if (args.Length > 0) lang.SetServerLanguage(args[0]);
            Reply(Lang("ServerLanguage", null, lang.GetServerLanguage()));
        }

        #endregion

        #region Group Command

        /// <summary>
        /// Called when the "group" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatGroup")]
        private void ChatGroup(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 2)
            {
                Reply(Lang("CommandUsageGroup", session.SteamId.ToString()), session);
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
                    Reply(Lang("GroupAlreadyExists", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.CreateGroup(name, title, rank);
                Reply(Lang("GroupCreated", session.SteamId.ToString(), name), session);
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.RemoveGroup(name);
                Reply(Lang("GroupDeleted", session.SteamId.ToString(), name), session);
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                Reply(Lang("GroupChanged", session.SteamId.ToString(), name), session);
            }
            else if (mode.Equals("parent"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                var parent = args[2];
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    Reply(Lang("GroupParentNotFound", session.SteamId.ToString(), parent), session);
                    return;
                }
                if (permission.SetGroupParent(name, parent))
                    Reply(Lang("GroupParentChanged", session.SteamId.ToString(), name, parent), session);
                else
                    Reply(Lang("GroupParentNotChanged", session.SteamId.ToString(), name), session);
            }
        }

        /// <summary>
        /// Called when the "group" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleGroup")]
        private void ConsoleGroup(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Reply(Lang("CommandUsageGroup"));
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
                    Reply(Lang("GroupAlreadyExists", null, name));
                    return;
                }
                permission.CreateGroup(name, title, rank);
                Reply(Lang("GroupCreated", null, name));
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", null, name));
                    return;
                }
                permission.RemoveGroup(name);
                Reply(Lang("GroupDeleted", null, name));
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", null, name));
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                Reply(Lang("GroupChanged", null, name));
            }
            else if (mode.Equals("parent"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", null, name));
                    return;
                }
                var parent = args[2];
                if (!string.IsNullOrEmpty(parent) && !permission.GroupExists(parent))
                {
                    Reply(Lang("GroupParentNotFound", null, parent));
                    return;
                }
                if (permission.SetGroupParent(name, parent))
                    Reply(Lang("GroupParentChanged", null, name, parent));
                else
                    Reply(Lang("GroupParentNotChanged", null, name));
            }
        }

        #endregion

        #region Usergroup Command

        /// <summary>
        /// Called when the "usergroup" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatUserGroup")]
        private void ChatUserGroup(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageUserGroup", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = FindSession(name);
            if (target == null && !permission.UserIdValid(name))
            {
                Reply(Lang("UserNotFound", session.SteamId.ToString(), name), session);
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.SteamId.ToString();
                name = target.Name;
                permission.UpdateNickname(userId, name);
            }

            if (!permission.GroupExists(group))
            {
                Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                Reply(Lang("UserAddedToGroup", session.SteamId.ToString(), name, group), session);
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                Reply(Lang("UserRemovedFromGroup", session.SteamId.ToString(), name, group), session);
            }
        }

        /// <summary>
        /// Called when the "usergroup" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleUserGroup")]
        private void ConsoleUserGroup(string command, string[] args)
        {
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageUserGroup"));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var group = args[2];

            var target = FindSession(name);
            if (target == null && !permission.UserIdValid(name))
            {
                Reply(Lang("UserNotFound", null, name));
                return;
            }
            var userId = name;
            if (target != null)
            {
                userId = target.SteamId.ToString();
                name = target.Name;
                permission.UpdateNickname(userId, name);
            }

            if (!permission.GroupExists(group))
            {
                Reply(Lang("GroupNotFound", null, name));
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                Reply(Lang("UserAddedToGroup", null, name, group));
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                Reply(Lang("UserRemovedFromGroup", null, name, group));
            }
        }

        #endregion

        #region Grant Command

        /// <summary>
        /// Called when the "grant" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatGrant")]
        private void ChatGrant(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageGrant", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(Lang("PermissionNotFound", session.SteamId.ToString(), perm), session);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                Reply(Lang("GroupPermissionGranted", session.SteamId.ToString(), name, perm), session);
            }
            else if (mode.Equals("user"))
            {
                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.GrantUserPermission(userId, perm, null);
                Reply(Lang("UserPermissionGranted", session.SteamId.ToString(), $"{name} ({userId})", perm), session);
            }
        }

        /// <summary>
        /// Called when the "grant" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleGrant")]
        private void ConsoleGrant(string command, string[] args)
        {
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageGrant"));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(Lang("PermissionNotFound", null, perm));
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", null, name));
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                Reply(Lang("GroupPermissionGranted", null, name, perm));
            }
            else if (mode.Equals("user"))
            {
                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", null, name));
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.GrantUserPermission(userId, perm, null);
                Reply(Lang("UserPermissionGranted", null, $"{name} ({userId})", perm));
            }
        }

        #endregion

        #region Revoke Command

        /// <summary>
        /// Called when the "revoke" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatRevoke")]
        private void ChatRevoke(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageRevoke", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(Lang("PermissionNotFound", session.SteamId.ToString(), perm), session);
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                Reply(Lang("GroupPermissionRevoked", session.SteamId.ToString(), name, perm), session);
            }
            else if (mode.Equals("user"))
            {
                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", session.SteamId.ToString(), name), session);
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.RevokeUserPermission(userId, perm);
                Reply(Lang("UserPermissionRevoked", session.SteamId.ToString(), $"{name} ({userId})", perm), session);
            }
        }

        /// <summary>
        /// Called when the "revoke" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleRevoke")]
        private void ConsoleRevoke(string command, string[] args)
        {
            if (args.Length < 3)
            {
                Reply(Lang("CommandUsageRevoke"));
                return;
            }

            var mode = args[0];
            var name = args[1];
            var perm = args[2];

            if (!permission.PermissionExists(perm))
            {
                Reply(Lang("PermissionNotFound", null, perm));
                return;
            }

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    Reply(Lang("GroupNotFound", null, name));
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                Reply(Lang("GroupPermissionRevoked", null, name, perm));
            }
            else if (mode.Equals("user"))
            {
                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", null, name));
                    return;
                }
                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                }
                permission.RevokeUserPermission(userId, perm);
                Reply(Lang("UserPermissionRevoked", null, $"{name} ({userId})", perm));
            }
        }

        #endregion

        #region Show Command

        /// <summary>
        /// Called when the "show" chat command has been executed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ChatShow")]
        private void ChatShow(PlayerSession session, string command, string[] args)
        {
            if (!PermissionsLoaded(session) || !IsAdmin(session)) return;
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageShow", session.SteamId.ToString()), session);
                return;
            }

            var mode = args[0];
            var name = args.Length > 1 ? args[1] : string.Empty;

            if (mode.Equals("perms"))
            {
                var result = "Permissions:\n";
                result += string.Join(", ", permission.GetPermissions());
                Reply(result, session);
            }
            else if (mode.Equals("user"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(Lang("CommandUsageShow", session.SteamId.ToString()), session);
                    return;
                }

                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound", session.SteamId.ToString()), session);
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }
                var result = $"User '{name}' permissions:\n";
                result += string.Join(", ", permission.GetUserPermissions(userId));
                result += $"\nUser '{name}' groups:\n";
                result += string.Join(", ", permission.GetUserGroups(userId));
                Reply(result, session);
            }
            else if (mode.Equals("group"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(Lang("CommandUsageShow", session.SteamId.ToString()), session);
                    return;
                }

                if (!permission.GroupExists(name) && !string.IsNullOrEmpty(name))
                {
                    Reply(Lang("GroupNotFound", session.SteamId.ToString(), name), session);
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
                Reply(result, session);
            }
            else if (mode.Equals("groups"))
            {
                Reply(Lang("ShowGroups", session.SteamId.ToString(), "\n" + string.Join(", ", permission.GetGroups())), session);
            }
        }

        /// <summary>
        /// Called when the "show" console command has been executed
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [HookMethod("ConsoleShow")]
        private void ConsoleShow(string command, string[] args)
        {
            if (args.Length < 1)
            {
                Reply(Lang("CommandUsageShow"));
                return;
            }

            var mode = args[0];
            var name = args.Length > 1 ? args[1] : string.Empty;

            if (mode.Equals("perms"))
            {
                var result = "Permissions:\n";
                result += string.Join(", ", permission.GetPermissions());
                Reply(result);
            }
            else if (mode.Equals("user"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(Lang("CommandUsageShow"));
                    return;
                }

                var target = FindSession(name);
                if (target == null && !permission.UserIdValid(name))
                {
                    Reply(Lang("UserNotFound"));
                    return;
                }

                var userId = name;
                if (target != null)
                {
                    userId = target.SteamId.ToString();
                    name = target.Name;
                    permission.UpdateNickname(userId, name);
                    name += $" ({userId})";
                }
                var result = $"User '{name}' permissions:\n";
                result += string.Join(", ", permission.GetUserPermissions(userId));
                result += $"\nUser '{name}' groups:\n";
                result += string.Join(", ", permission.GetUserGroups(userId));
                Reply(result);
            }
            else if (mode.Equals("group"))
            {
                if (string.IsNullOrEmpty(name))
                {
                    Reply(Lang("CommandUsageShow"));
                    return;
                }

                if (!permission.GroupExists(name) && !string.IsNullOrEmpty(name))
                {
                    Reply(Lang("GroupNotFound", null, name));
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
                Reply(result);
            }
            else if (mode.Equals("groups"))
            {
                Reply(Lang("ShowGroups", null, "\n" + string.Join(", ", permission.GetGroups())));
            }
        }

        #endregion

        #endregion

        #region Command Handling

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [HookMethod("OnServerCommand")]
        private object OnServerCommand(string arg)
        {
            if (arg == null || arg.Trim().Length == 0) return null;

            var command = $"{arg.Split(' ')[0]}";
            var args = arg.Split(' ').Skip(1).ToArray();

            // Is this a covalence command?
            if (Covalence.CommandSystem.HandleConsoleMessage(Covalence.CommandSystem.consolePlayer, arg)) return true;

            return cmdlib.HandleConsoleCommand(command, args);
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
        /// Returns if specified player is admin
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private bool IsAdmin(PlayerSession session)
        {
            if (session == null || session.IsAdmin) return true;
            Reply(Lang("YouAreNotAdmin", session.SteamId.ToString()), session);
            return false;
        }

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="session"></param>
        /// <param name="args"></param>
        private static void Reply(string message, PlayerSession session = null, params object[] args)
        {
            if (session == null || !session.Player.isConnected)
            {
                Interface.Oxide.LogInfo(message, args);
                return;
            }
            ChatManagerServer.Instance.RPC("RelayChat", session.Player, string.Format(message, args));
        }

        /// <summary>
        /// Returns the localized message from key using optional ID string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="args"></param>
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        /// <summary>
        /// Gets the player session using a name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public PlayerSession FindSession(string nameOrIdOrIp)
        {
            var sessions = GameManager.Instance.GetSessions();
            PlayerSession session = null;
            foreach (var i in sessions)
            {
                if (!nameOrIdOrIp.Equals(i.Value.Name, StringComparison.OrdinalIgnoreCase) &&
                    !nameOrIdOrIp.Equals(i.Value.SteamId.ToString()) && !nameOrIdOrIp.Equals(i.Key.ipAddress)) continue;
                session = i.Value;
                break;
            }
            return session;
        }

        /// <summary>
        /// Gets the player session using a uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerSession FindSessionByNetPlayer(uLink.NetworkPlayer player) => GameManager.Instance.GetSession(player);

        /// <summary>
        /// Gets the player session using a UnityEngine.GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public PlayerSession FindSessionByGo(GameObject go)
        {
            var sessions = GameManager.Instance.GetSessions();
            return (from i in sessions where go.Equals(i.Value.WorldPlayerEntity) select i.Value).FirstOrDefault();
        }

        #endregion
    }
}
