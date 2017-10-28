using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.RustLegacy.Libraries;
using Oxide.Game.RustLegacy.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// The core Rust Legacy plugin
    /// </summary>
    public partial class RustLegacyCore : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the RustCore class
        /// </summary>
        public RustLegacyCore()
        {
            // Set attributes
            Title = "Rust Legacy";
            Author = RustLegacyExtension.AssemblyAuthors;
            Version = RustLegacyExtension.AssemblyVersion;
        }

        // Libraries
        internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        //internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly RustLegacyCovalenceProvider Covalence = RustLegacyCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = Covalence.CreateServer();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            "rcon.login", "rcon.password"
        };

        private bool serverInitialized;
        private float lastWarningAt; // Last Metabolism hacker notification time

        public class PlayerData
        {
            public Character character;
            public PlayerInventory inventory;
        }

        // Cache some player information
        private static readonly Dictionary<NetUser, PlayerData> playerData = new Dictionary<NetUser, PlayerData>();

        #endregion Initialization

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
            AddCovalenceCommand(new[] { "oxide.plugins", "o.plugins", "plugins" }, "PluginsCommand", "oxide.plugins");
            AddCovalenceCommand(new[] { "oxide.load", "o.load", "plugin.load" }, "LoadCommand", "oxide.load");
            AddCovalenceCommand(new[] { "oxide.reload", "o.reload", "plugin.reload" }, "ReloadCommand", "oxide.reload");
            AddCovalenceCommand(new[] { "oxide.unload", "o.unload", "plugin.unload" }, "UnloadCommand", "oxide.unload");

            // Add core permission commands
            AddCovalenceCommand(new[] { "oxide.grant", "o.grant", "perm.grant" }, "GrantCommand", "oxide.grant");
            AddCovalenceCommand(new[] { "oxide.group", "o.group", "perm.group" }, "GroupCommand", "oxide.group");
            AddCovalenceCommand(new[] { "oxide.revoke", "o.revoke", "perm.revoke" }, "RevokeCommand", "oxide.revoke");
            AddCovalenceCommand(new[] { "oxide.show", "o.show", "perm.show" }, "ShowCommand", "oxide.show");
            AddCovalenceCommand(new[] { "oxide.usergroup", "o.usergroup", "perm.usergroup" }, "UserGroupCommand", "oxide.usergroup");

            // Add core misc commands
            AddCovalenceCommand(new[] { "oxide.lang", "o.lang" }, "LangCommand");
            AddCovalenceCommand(new[] { "oxide.version", "o.version" }, "VersionCommand");

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
            RustLegacyExtension.ServerConsole();

            serverInitialized = true;
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        #endregion Core Hooks

        #region Command Handling

        /// <summary>
        /// Parses the specified chat command
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
                        inlongarg = true;
                }
                else if (char.IsWhiteSpace(c) && !inlongarg)
                {
                    var arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                    sb = new StringBuilder();
                }
                else
                    sb.Append(c);
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
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="wantreply"></param>
        /// <returns></returns>
        [HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg, bool wantreply)
        {
            if (arg == null) return null;

            var cmdFullName = $"{arg.Class}.{arg.Function}";
            var str = arg.ArgsStr.Trim('"');
            var netUser = arg.argUser;

            // Get the covalence player
            var iplayer = netUser != null ? Covalence.PlayerManager.FindPlayerById(netUser.userID.ToString()) : null;

            // Is it a console command?
            if (cmdFullName != "chat.say")
            {
                if (Covalence.CommandSystem.HandleConsoleMessage(iplayer, $"{cmdFullName} {str}") || cmdlib.HandleConsoleCommand(arg, wantreply)) return true;
                return null;
            }

            if (str.Length == 0) return true;

            // Is it a chat command?
            if (str[0] != '/')
            {
                var chatSpecific = Interface.Call("OnPlayerChat", netUser, str);
                var chatCovalence = Interface.Call("OnUserChat", iplayer, str);
                return chatSpecific ?? chatCovalence;
            }

            // Get the full command
            var command = str.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseCommand(command, out cmd, out args);
            if (cmd == null) return true;

            // Get the covalence player
            if (iplayer == null) return null;

            // Is the command blocked?
            var commandSpecific = Interface.Call("OnPlayerCommand", arg);
            var commandCovalence = Interface.Call("OnUserCommand", iplayer, cmd, args); // TODO: Fix NRE from chat.say /x via console
            if (commandSpecific != null || commandCovalence != null) return true;

            // Is this a Covalence command?
            if (Covalence.CommandSystem.HandleChatMessage(iplayer, str)) return true; // TODO: Fix NRE from chat.say /x via console

            // Is it a regular chat command?
            if (netUser == null)
                Interface.Oxide.LogDebug("Player is actually a {netUser}!");
            else if (!cmdlib.HandleChatCommand(netUser, cmd, args))
                ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"chat.add \"Server\" \" Unknown command {cmd}\"");

            // Handled
            arg.ReplyWith(string.Empty);
            return true;
        }

        #endregion Command Handling

        #region Game Fixes

        /// <summary>
        /// Called when the GetClientMove packet is received for the player
        /// Checks the player position in the packet to prevent harmful packets crashing the server
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="pos"></param>
        [HookMethod("IOnGetClientMove")]
        private object IOnGetClientMove(NetUser netUser, Vector3 pos)
        {
            if (float.IsNaN(pos.x) || float.IsInfinity(pos.x) || float.IsNaN(pos.y) || float.IsInfinity(pos.y) || float.IsNaN(pos.z) || float.IsInfinity(pos.z))
            {
                Interface.Oxide.LogInfo($"Banned {netUser.displayName} [{netUser.userID}] for sending bad packets (possible teleport hack)");
                BanList.Add(netUser.userID, netUser.displayName, "Sending bad packets (possible teleport hack)");
                netUser.Kick(NetError.ConnectionBanned, true);
                return false;
            }

            return Interface.Oxide.CallHook("OnPlayerMove", netUser, pos);
        }

        /// <summary>
        /// Called when receiving an RPC message from a client attempting to run RecieveNetwork on the server
        /// This shouldn't run from the server ever and is only used by metabolism hacks
        /// </summary>
        [HookMethod("IOnRecieveNetwork")]
        private object IOnRecieveNetwork()
        {
            var now = Interface.Oxide.Now;
            if (now - lastWarningAt > 300f)
            {
                lastWarningAt = now;
                Interface.Oxide.LogInfo("An attempt to use a metabolism hack was prevented.");
            }
            return false;
        }

        /// <summary>
        /// Called when an error is thrown because of an invalid RPC message
        /// </summary>
        /// <param name="obj"></param>
        [HookMethod("IOnRPCError")]
        private void IOnRPCError(object obj)
        {
            var info = obj as uLink.NetworkMessageInfo;
            if (info == null) return;

            if (info.sender == uLink.NetworkPlayer.server) return;

            var netuser = info.sender.localData as NetUser;
            if (netuser == null) return;

            Interface.Oxide.LogWarning($"An RPC message from {netuser.displayName} has triggered an exception. Kicking the player...");
            if (netuser.connected) netuser.Kick(NetError.Facepunch_Kick_Violation, true);
        }

        /// <summary>
        /// Called when an AI moves
        /// Checking the NavMeshPathStatus, if the path is invalid the AI is killed to stop NavMesh errors
        /// </summary>
        /// <param name="ai"></param>
        /// <param name="movement"></param>
        [HookMethod("IOnAIMovement")]
        private void IOnAIMovement(BasicWildLifeAI ai, BaseAIMovement movement)
        {
            var nmMovement = movement as NavMeshMovement;
            if (!nmMovement) return;

            if (nmMovement._agent.pathStatus == NavMeshPathStatus.PathInvalid && ai.GetComponent<TakeDamage>().alive)
            {
                TakeDamage.KillSelf(ai.GetComponent<IDBase>());
                Interface.Oxide.LogInfo($"{ai} was destroyed for having an invalid NavMeshPath");
            }
        }

        #endregion Game Fixes

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

        // TODO: Deprecate and remove
        public NetUser FindPlayer(string nameOrIdOrIp)
        {
            NetUser netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.userID.ToString() == nameOrIdOrIp)?.netUser) != null) return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.displayName.ToLower().Contains(nameOrIdOrIp.ToLower()))?.netUser) != null) return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.networkPlayer.ipAddress == nameOrIdOrIp)?.netUser) != null) return netUser;
            return null;
        }

        // TODO: Deprecate and remove
        public static Character GetCharacter(NetUser netUser) => netUser?.playerClient?.controllable?.GetComponent<Character>();

        // TODO: Deprecate and remove
        public static PlayerInventory GetInventory(NetUser netUser) => RustLegacyCore.GetCharacter(netUser)?.GetComponent<PlayerInventory>();

        #endregion Helpers
    }
}
