using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
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
    public partial class HurtworldCore : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the HurtworldCore class
        /// </summary>
        public HurtworldCore()
        {
            Title = "Hurtworld";
            Author = "Oxide Team";
            var assemblyVersion = HurtworldExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        // Libraries
        internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly HurtworldCovalenceProvider Covalence = HurtworldCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = Covalence.CreateServer();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            "bindip", "host", "queryport"
        };

        private bool serverInitialized;

        // Track 'load' chat commands
        private readonly List<string> loadingPlugins = new List<string>();

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
            HurtworldExtension.ServerConsole();
            SteamGameServer.SetGameTags("oxide,modded");

            serverInitialized = true;
        }
 
        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion

        #region Player Hooks

        /// <summary>
        /// Called when the player is attempting to connect
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

            // Update player's permissions group and name
            if (permission.IsLoaded)
            {
                var id = session.SteamId.ToString();
                permission.UpdateNickname(id, name);
                var defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(id, defaultGroups.Players)) permission.AddUserGroup(id, defaultGroups.Players);
                if (session.IsAdmin && !permission.UserHasGroup(id, defaultGroups.Administrators)) permission.AddUserGroup(id, defaultGroups.Administrators);
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
        /// Called when the server receives input from the player
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
        /// Called when the player tries to enter a vehicle
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
        /// Called when the player tries to exit a vehicle
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
        /// Called when the player enters a vehicle
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
        /// Called when the player exits a vehicle
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
                    if (ent != null) Interface.CallHook("OnEntityTakeDamage", ent, source);
                    break;
                case EEntityFluidEffectType.CreatureToPlayerDamage:
                case EEntityFluidEffectType.Damage:
                case EEntityFluidEffectType.FallDamageProxy:
                    if (target.GetComponent<AIEntity>() != null) break;
                    var networkView = target.GetComponent<uLinkNetworkView>();
                    if (networkView == null) break;
                    var session = GameManager.Instance.GetSession(networkView.owner);
                    if (session != null) Interface.CallHook("OnPlayerTakeDamage", session, source);
                    break;
            }
        }

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
