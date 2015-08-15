using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.RustLegacy.Libraries;

using Rust.Defines;
using uLink;
using UnityEngine;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// The core Rust Legacy plugin
    /// </summary>
    public class RustLegacyCore : CSPlugin
    {
        // The pluginmanager
        private readonly PluginManager pluginmanager = Interface.Oxide.RootPluginManager;

        // The permission lib
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>("Permission");

        private static readonly string[] DefaultGroups = { "player", "admin" };

        // The command lib
        private readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        // Track when the server has been initialized
        private bool ServerInitialized;

        // Cache the VoiceCom.playerList field info
        private readonly FieldInfo playerList = typeof(VoiceCom).GetField("playerList", BindingFlags.Static | BindingFlags.NonPublic);

        // Cache the DamageEvent.takenodamage field info
        readonly FieldInfo takenodamage = typeof(TakeDamage).GetField("takenodamage", BindingFlags.NonPublic | BindingFlags.Instance);

        // Last Metabolism hacker notification time
        float lastWarningAt;

        /// <summary>
        /// Initializes a new instance of the RustCore class
        /// </summary>
        public RustLegacyCore()
        {
            // Set attributes
            Name = "rustlegacycore";
            Title = "Rust Legacy Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);
        }

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Add our commands
            cmdlib.AddConsoleCommand("oxide.plugins", this, "cmdPlugins");
            cmdlib.AddConsoleCommand("global.plugins", this, "cmdPlugins");
            cmdlib.AddConsoleCommand("oxide.load", this, "cmdLoad");
            cmdlib.AddConsoleCommand("global.load", this, "cmdLoad");
            cmdlib.AddConsoleCommand("oxide.unload", this, "cmdUnload");
            cmdlib.AddConsoleCommand("global.unload", this, "cmdUnload");
            cmdlib.AddConsoleCommand("oxide.reload", this, "cmdReload");
            cmdlib.AddConsoleCommand("global.reload", this, "cmdReload");
            cmdlib.AddConsoleCommand("oxide.version", this, "cmdVersion");
            cmdlib.AddConsoleCommand("global.version", this, "cmdVersion");

            cmdlib.AddConsoleCommand("oxide.group", this, "cmdGroup");
            cmdlib.AddConsoleCommand("global.group", this, "cmdGroup");
            cmdlib.AddConsoleCommand("oxide.usergroup", this, "cmdUserGroup");
            cmdlib.AddConsoleCommand("global.usergroup", this, "cmdUserGroup");
            cmdlib.AddConsoleCommand("oxide.grant", this, "cmdGrant");
            cmdlib.AddConsoleCommand("global.grant", this, "cmdGrant");
            cmdlib.AddConsoleCommand("oxide.revoke", this, "cmdRevoke");
            cmdlib.AddConsoleCommand("global.revoke", this, "cmdRevoke");

            // Setup the default permission groups
            if (permission.IsLoaded)
            {
                var rank = 0;
                for (var i = DefaultGroups.Length - 1; i >= 0; i--)
                {
                    var defaultGroup = DefaultGroups[i];
                    if (!permission.GroupExists(defaultGroup)) permission.CreateGroup(defaultGroup, defaultGroup, rank++);
                }
            }

            // Configure remote logging
            RemoteLogger.SetTag("game", "rust legacy");
            RemoteLogger.SetTag("protocol", Connection.protocol.ToString());
        }

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <returns></returns>
        private bool PermissionsLoaded(ConsoleSystem.Arg arg)
        {
            if (permission.IsLoaded) return true;
            arg.ReplyWith("Unable to load permission files! Permissions will not work until the error has been resolved.\r\n => " + permission.LastException.Message);
            return false;
        }

        /// <summary>
        /// Called when the server is first initialized
        /// </summary>
        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            if (ServerInitialized) return;
            ServerInitialized = true;
            // Configure the hostname after it has been set
            RemoteLogger.SetTag("hostname", server.hostname);
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
        /// Called when another plugin has been loaded
        /// </summary>
        /// <param name="plugin"></param>
        [HookMethod("OnPluginLoaded")]
        private void OnPluginLoaded(Plugin plugin)
        {
            if (ServerInitialized) plugin.CallHook("OnServerInitialized");
        }

        /// <summary>
        /// Called when the "oxide.plugins" command has been executed
        /// </summary>
        [HookMethod("cmdPlugins")]
        private void cmdPlugins(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !arg.argUser.admin) return;

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
                arg.ReplyWith("[Oxide] No plugins are currently available");
                return;
            }

            var output = $"[Oxide] Listing {loaded_plugins.Length + unloaded_plugin_errors.Count} plugins:";
            var number = 1;
            output = loaded_plugins.Aggregate(output, (current, plugin) => current + $"\n  {number++:00} \"{plugin.Title}\" ({plugin.Version}) by {plugin.Author}");
            output = unloaded_plugin_errors.Keys.Aggregate(output, (current, plugin_name) => current + $"\n  {number++:00} {plugin_name} - {unloaded_plugin_errors[plugin_name]}");
            arg.ReplyWith(output);
        }

        /// <summary>
        /// Called when the "oxide.load" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdLoad")]
        private void cmdLoad(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !arg.argUser.admin) return;
            // Check arg 1 exists
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: oxide.load *|<pluginname>+");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.LoadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
            {
                if (string.IsNullOrEmpty(name)) continue;
                // Load
                Interface.Oxide.LoadPlugin(name);
                pluginmanager.GetPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "oxide.unload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdUnload")]
        private void cmdUnload(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !arg.argUser.admin) return;
            // Check arg 1 exists
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: oxide.unload *|<pluginname>+");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.UnloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                // Unload
                Interface.Oxide.UnloadPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "oxide.reload" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdReload")]
        private void cmdReload(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null && !arg.argUser.admin) return;
            // Check arg 1 exists
            if (!arg.HasArgs())
            {
                arg.ReplyWith("Syntax: oxide.reload *|<pluginname>+");
                return;
            }

            if (arg.GetString(0).Equals("*"))
            {
                Interface.Oxide.ReloadAllPlugins();
                return;
            }

            foreach (var name in arg.Args)
            {
                if (string.IsNullOrEmpty(name)) continue;

                // Reload
                Interface.Oxide.ReloadPlugin(name);
            }
        }

        /// <summary>
        /// Called when the "version" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdVersion")]
        private void cmdVersion(ConsoleSystem.Arg arg)
        {
            // Get the Rust network protocol version
            var protocol = Connection.protocol.ToString();

            // Get the Oxide Core version
            var oxide = OxideMod.Version.ToString();

            // Show the versions
            if (!string.IsNullOrEmpty(protocol) && !string.IsNullOrEmpty(oxide))
            {
                arg.ReplyWith($"Oxide Version: {oxide}, Rust Protocol: {protocol}");
            }
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdGroup")]
        private void cmdGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;

            if (arg.argUser != null && !arg.argUser.CanAdmin()) return;
            // Check 2 args exists
            if (!arg.HasArgs(2))
            {
                arg.ReplyWith("Syntax: oxide.group <add|remove|set> <name> [title] [rank]");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var title = arg.GetString(2);
            var rank = arg.GetInt(3);

            if (mode.Equals("add"))
            {
                if (permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' already exist");
                    return;
                }
                permission.CreateGroup(name, title, rank);
                arg.ReplyWith("Group '" + name + "' created");
            }
            else if (mode.Equals("remove"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RemoveGroup(name);
                arg.ReplyWith("Group '" + name + "' deleted");
            }
            else if (mode.Equals("set"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.SetGroupTitle(name, title);
                permission.SetGroupRank(name, rank);
                arg.ReplyWith("Group '" + name + "' changed");
            }
        }

        /// <summary>
        /// Called when the "group" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdUserGroup")]
        private void cmdUserGroup(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;

            if (arg.argUser != null && !arg.argUser.CanAdmin()) return;
            // Check 3 args exists
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: oxide.usergroup <add|remove> <username> <groupname>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var group = arg.GetString(2);

            var player = FindPlayer(name);
            if (player == null && !permission.UserExists(name))
            {
                arg.ReplyWith("User '" + name + "' not found");
                return;
            }
            var userId = name;
            if (player != null)
            {
                userId = player.userID.ToString();
                name = player.displayName;
                permission.GetUserData(userId).LastSeenNickname = name;
            }

            if (!permission.GroupExists(group))
            {
                arg.ReplyWith("Group '" + group + "' doesn't exist");
                return;
            }

            if (mode.Equals("add"))
            {
                permission.AddUserGroup(userId, group);
                if (player != null)
                {
                    arg.ReplyWith("User '" + player.displayName + "' assigned group: " + @group);
                }
            }
            else if (mode.Equals("remove"))
            {
                permission.RemoveUserGroup(userId, group);
                if (player != null)
                {
                    arg.ReplyWith("User '" + player.displayName + "' removed from group: " + @group);
                }
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdGrant")]
        private void cmdGrant(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;

            if (arg.argUser != null && !arg.argUser.CanAdmin()) return;
            // Check 3 args exists
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: oxide.grant <group|user> <name|id> <permission>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.GrantGroupPermission(name, perm, null);
                arg.ReplyWith("Group '" + name + "' granted permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var player = FindPlayer(name);
                if (player == null && !permission.UserExists(name))
                {
                    arg.ReplyWith("User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (player != null)
                {
                    userId = player.userID.ToString();
                    name = player.displayName;
                    permission.GetUserData(name).LastSeenNickname = name;
                }
                permission.GrantUserPermission(userId, perm, null);
                arg.ReplyWith("User '" + name + "' granted permission: " + perm);
            }
        }

        /// <summary>
        /// Called when the "grant" command has been executed
        /// </summary>
        /// <param name="arg"></param>
        [HookMethod("cmdRevoke")]
        private void cmdRevoke(ConsoleSystem.Arg arg)
        {
            if (!PermissionsLoaded(arg)) return;

            if (arg.argUser != null && !arg.argUser.CanAdmin()) return;
            // Check 3 args exists
            if (!arg.HasArgs(3))
            {
                arg.ReplyWith("Syntax: oxide.revoke <group|user> <name|id> <permission>");
                return;
            }

            var mode = arg.GetString(0);
            var name = arg.GetString(1);
            var perm = arg.GetString(2);

            if (mode.Equals("group"))
            {
                if (!permission.GroupExists(name))
                {
                    arg.ReplyWith("Group '" + name + "' doesn't exist");
                    return;
                }
                permission.RevokeGroupPermission(name, perm);
                arg.ReplyWith("Group '" + name + "' revoked permission: " + perm);
            }
            else if (mode.Equals("user"))
            {
                var player = FindPlayer(name);
                if (player == null && !permission.UserExists(name))
                {
                    arg.ReplyWith("User '" + name + "' not found");
                    return;
                }
                var userId = name;
                if (player != null)
                {
                    userId = player.userID.ToString();
                    name = player.displayName;
                    permission.GetUserData(name).LastSeenNickname = name;
                }
                permission.RevokeUserPermission(userId, perm);
                arg.ReplyWith("User '" + name + "' revoked permission: " + perm);
            }
        }

        public NetUser FindPlayer(string strNameOrIdorIp)
        {
            NetUser netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.userID.ToString() == strNameOrIdorIp)?.netUser) != null)
                return netUser;

            if ((netUser = PlayerClient.All.Find(p => p.netUser.displayName.ToLower().Contains(strNameOrIdorIp.ToLower()))?.netUser) != null)
                return netUser;

            if ((netUser = PlayerClient.All.Find(p => p.netUser.networkPlayer.ipAddress == strNameOrIdorIp)?.netUser) != null)
                return netUser;

            return null;
        }

        /// <summary>
        /// Called when the server wants to know what tags to use
        /// </summary>
        /// <param name="oldtags"></param>
        /// <returns></returns>
        [HookMethod("IModifyTags")]
        private string IModifyTags(string oldtags)
        {
            // We're going to call out and build a list of all tags to use
            var taglist = new List<string>(oldtags.Split(','));
            Interface.CallHook("BuildServerTags", taglist);
            return string.Join(",", taglist.ToArray());
        }

        /// <summary>
        /// Called when it's time to build the tags list
        /// </summary>
        /// <param name="taglist"></param>
        [HookMethod("BuildServerTags")]
        private void BuildServerTags(IList<string> taglist)
        {
            // Add modded and oxide
            taglist.Add("modded");
            taglist.Add("oxide");
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
            // Sanity checks
            if (arg == null) return null;

            var cmdlib = Interface.Oxide.GetLibrary<Command>("Command");
            string cmd = $"{arg.Class}.{arg.Function}";

            // Is it chat.say?
            if (cmd == "chat.say")
            {
                // Get the args
                var str = arg.GetString(0);
                if (str.Length == 0) return true;

                // Is it a chat command?
                if (str[0] == '/' || str[0] == '!')
                {
                    // Get the arg string
                    var argstr = str.Substring(1);
                    if (str.Length == 1) return true;

                    // Parse it
                    string chatcmd;
                    string[] args;
                    ParseChatCommand(argstr, out chatcmd, out args);
                    if (chatcmd == null) return null;

                    // Handle it
                    var ply = arg.argUser;
                    if (ply != null && !cmdlib.HandleChatCommand(ply, chatcmd, args))
                    {
                        ConsoleNetworker.SendClientCommand(ply.networkPlayer, $"chat.add \"Server\" \" Unknown command {chatcmd}\"");
                        return true;
                    }

                    // Handled
                    arg.ReplyWith(string.Empty);
                    return true;
                }
                return Interface.CallHook("OnPlayerChat", arg.argUser, str);
            }

            return cmdlib.HandleConsoleCommand(arg, wantreply);
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
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="approval"></param>
        /// <param name="acceptor"></param>
        [HookMethod("IOnUserApprove")]
        private object IOnUserApprove(ClientConnection connection, NetworkPlayerApproval approval, ConnectionAcceptor acceptor)
        {
            // Reject invalid connections
            if (connection.UserID == 0 || string.IsNullOrEmpty(connection.UserName))
            {
                approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                return false;
            }

            var result = Interface.CallHook("CanClientLogin", connection, approval);
            if (result is uLink.NetworkConnectionError)
            {
                approval.Deny((uLink.NetworkConnectionError)result);
                return false;
            }
            return Interface.CallHook("OnUserApprove", connection, approval, acceptor);
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerConnected")]
        private void OnPlayerConnected(NetUser player)
        {
            if (!permission.IsLoaded) return;
            var userId = player.userID.ToString();
            var userData = permission.GetUserData(userId);
            userData.LastSeenNickname = player.displayName;
            if (userData.Groups.Count > 0) return;
            permission.AddUserGroup(userId, DefaultGroups[0]);
        }

        /// <summary>
        /// Called when a user is speaking
        /// </summary>
        /// <param name="client"></param>
        /// <param name="total"></param>
        [HookMethod("IOnPlayerVoice")]
        private object IOnPlayerVoice(PlayerClient client, int total)
        {
            var players = (List<uLink.NetworkPlayer>)playerList.GetValue(null);
            var num = Interface.CallHook("OnPlayerVoice", client.netUser, players);
            playerList.SetValue(null, players);
            return (num != null) ? (int) num : num;
        }

        /// <summary>
        /// Called when a user places a structure in the world
        /// </summary>
        /// <param name="component"></param>
        /// <param name="item"></param>
        [HookMethod("IOnStructureBuilt")]
        private object IOnStructureBuilt(StructureComponent component, IStructureComponentItem item)
        {
            return Interface.CallHook("OnStructureBuilt", component, item.controllable.netUser);
        }

        /// <summary>
        /// Called when the user puts a deployable in the world
        /// </summary>
        /// <param name="component"></param>
        /// <param name="item"></param>
        [HookMethod("IOnItemDeployed")]
        private object IOnItemDeployed(DeployableObject component, IDeployableItem item)
        {
            return Interface.CallHook("OnItemDeployed", component, item.controllable.netUser);
        }

        /// <summary>
        /// Called when damage is processed
        /// </summary>
        /// <param name="takedamage"></param>
        /// <param name="damage"></param>
        [HookMethod("IOnProcessDamageEvent")]
        private object IOnProcessDamageEvent(TakeDamage takedamage, DamageEvent damage)
        {
            var dmg = Interface.CallHook("ModifyDamage", takedamage, damage);
            if (dmg is DamageEvent)
                damage = (DamageEvent)dmg;
            if ((bool)takenodamage.GetValue(takedamage)) return null;

            var lifeStatus = damage.status;
            if (lifeStatus == LifeStatus.WasKilled)
            {
                takedamage.health = 0f;
                Interface.CallHook("OnKilled", takedamage, damage);
            }
            else if (lifeStatus == LifeStatus.IsAlive)
            {
                takedamage.health -= damage.amount;
                Interface.CallHook("OnHurt", takedamage, damage);
            }

            return damage;
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
            if (!nmMovement)
                return;

            var alive = ai.GetComponent<TakeDamage>().alive;

            if (nmMovement._agent.pathStatus == NavMeshPathStatus.PathInvalid && alive)
            {
                TakeDamage.KillSelf(ai.GetComponent<IDBase>());
                Interface.Oxide.LogInfo($"{ai} was destroyed for having an invalid NavMeshPath");
            }
        }

        /// <summary>
        /// Called when the GetClientMove packed is received for a player
        /// Checking the player position in the packet to prevent harmful packets crashing the server
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="origin"></param>
        /// <param name="encoded"></param>
        /// <param name="stateFlags"></param>
        /// <param name="info"></param>
        [HookMethod("IOnGetClientMove")]
        private object IOnGetClientMove(HumanController controller, Vector3 origin, int encoded, ushort stateFlags, uLink.NetworkMessageInfo info)
        {
            if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) ||
                float.IsNaN(origin.y) || float.IsInfinity(origin.y) ||
                float.IsNaN(origin.z) || float.IsInfinity(origin.z))
            {
                Interface.Oxide.LogInfo($"Banned {controller.netUser.displayName} [{controller.netUser.userID}] for sending bad packets (possible teleport hack)");
                BanList.Add(controller.netUser.userID, controller.netUser.displayName, "Sending bad packets (possible teleport hack)");
                controller.netUser.Kick(NetError.ConnectionBanned, true);
                return false;
            }

            return null;
        }

        /// <summary>
        /// Called when receiving an RPC message from a client attempting to run RecieveNetwork on the server
        /// This shouldn't run from the server ever and is only used by metabolism hacks
        /// </summary>
        /// <param name="metabolism"></param>
        /// <param name="calories"></param>
        /// <param name="water"></param>
        /// <param name="radiation"></param>
        /// <param name="antiradiation"></param>
        /// <param name="temperature"></param>
        /// <param name="poison"></param>
        [HookMethod("IOnRecieveNetwork")]
        private object IOnRecieveNetwork(Metabolism metabolism, float calories, float water, float radiation, float antiradiation, float temperature, float poison)
        {
            var now = Interface.Oxide.Now;
            if (now - lastWarningAt > 300f)
            {
                lastWarningAt = now;
                Interface.Oxide.LogInfo("An attempt to use a metabolism hack was prevented.");
            }
            return false;
        }
    }
}
