﻿using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.SpaceEngineers.Libraries;
using Oxide.Game.SpaceEngineers.Libraries.Covalence;
using VRage.Game;
using Sandbox;
using Sandbox.Engine.Utils;
using SteamSDK;
using System.Text;
using VRage.Game.ModAPI;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using System.Reflection;
using VRage.Network;
using VRage.Utils;
using Oxide.Game.SpaceEngineers.Logging;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;

namespace Oxide.Game.SpaceEngineers
{
    /// <summary>
    /// The core Space Engineers plugin
    /// </summary>
    public class SpaceEngineersCore : CSPlugin
    {
        #region Initialization

        // The permission library
        private readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        private static readonly string[] DefaultGroups = { "default", "moderator", "admin" };

        // Track 'load' chat commands
        private readonly List<string> loadingPlugins = new List<string>();

        // The covalence provider
        internal static readonly SpaceEngineersCovalenceProvider Covalence = SpaceEngineersCovalenceProvider.Instance;

        //// The command library
        internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();

        //// The player library
        internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Track when the server has been initialized
        private bool serverInitialized;
        private bool loggingInitialized;

        private SpaceEngineersLogger logger;

        private int m_totalTimeInMilliseconds;

        public static string[] Filter =
        {
        };

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

        /// <summary>
        /// Initializes a new instance of the SpaceEngineersCore class
        /// </summary>
        public SpaceEngineersCore()
        {
            var assemblyVersion = SpaceEngineersExtension.AssemblyVersion;

            // Set attributes
            Name = "SpaceEngineersCore";
            Title = "Space Engineers";
            Author = "Oxide Team";
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);

            var plugins = Interface.Oxide.GetLibrary<Core.Libraries.Plugins>();
        }

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            loggingInitialized = true;
            CallHook("InitLogging", null);

            Interface.Oxide.NextTick(() =>
            {
                logger = new SpaceEngineersLogger();
                Interface.Oxide.RootLogger.AddLogger(logger);
                Interface.Oxide.RootLogger.DisableCache();
            });
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
            RemoteLogger.SetTag("game version", MyFinalBuildConstants.APP_VERSION.FormattedText.ToString().Replace('_', '.'));

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
            if (!loggingInitialized) InitializeLogging();
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

            Analytics.Collect();

            //SteamSDK.SteamServerAPI.Instance.GameServer.SetGameTags(
            //    "groupId" + m_groupId.ToString() +
            //    " version" + MyFinalBuildConstants.APP_VERSION.ToString() +
            //    " datahash" + MyDataIntegrityChecker.GetHashBase64() +
            //    " " + MyMultiplayer.ModCountTag + ModCount +
            //    " gamemode" + gamemode +
            //    " " + MyMultiplayer.ViewDistanceTag + ViewDistance);
            //}
            //SteamGameServer.SetGameTags("oxide,modded");
            // Update server console window and status bars
            SpaceEngineersExtension.ServerConsole();
        }

        /// <summary>
        /// Called when the server is saving
        /// </summary>
        [HookMethod("OnServerSave")]
        private void OnServerSave() => Analytics.Collect();

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("IOnServerShutdown")]
        private void IOnServerShutdown()
        {
            Interface.Call("OnServerShutdown");
            Interface.Oxide.OnShutdown();
        }

        /// <summary>
        /// Called when the server is created sandbox
        /// </summary>
        [HookMethod("IOnSandboxCreated")]
        private void IOnSandboxCreated()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Utilities == null || MyAPIGateway.Multiplayer == null)
                return;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(0xff20, OnChatMessageFromClient);
        }

        private void OnChatMessageFromClient(byte[] bytes)
        {
            Interface.Oxide.LogInfo($"OnChatMessageFromClient {bytes}");
            string data = System.Text.Encoding.Unicode.GetString(bytes);
            var args = data.Split(new char[] { ',' }, 2);
            Interface.Oxide.LogInfo($"OnChatMessageFromClient {args[0]} {args[1]}");
            ulong steamId = 0;
            if (ulong.TryParse(args[0], out steamId))
                IOnPlayerChat(steamId, args[1], ChatEntryTypeEnum.ChatMsg);
            else
                Interface.Oxide.LogError($" can't parse steam id...");
        }

        /// <summary>
        /// Called when the server is created sandbox
        /// </summary>
        [HookMethod("IOnNextTick")]
        private void IOnNextTick()
        {
            var deltaTime = MySandboxGame.TotalTimeInMilliseconds - m_totalTimeInMilliseconds / 1000.0f;
            m_totalTimeInMilliseconds = MySandboxGame.TotalTimeInMilliseconds;
            Interface.Oxide.OnFrame(deltaTime);
        }

        /// <summary>
        /// Called when the server logs append
        /// </summary>
        [HookMethod("OnWriteLine")]
        private void OnWriteLine(string message)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

            var color = ConsoleColor.Gray;
            //Interface.Oxide.ServerConsole.AddMessage(message, color);
        }


        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(ulong steamUserID, string message, ChatEntryTypeEnum chatEntryTypee)
        {
            if (Sync.MyId == steamUserID) return true;

            Interface.Oxide.ServerConsole.AddMessage($"IOnPlayerChat {steamUserID} {message} {chatEntryTypee}", ConsoleColor.Yellow);
            if (message.Trim().Length <= 1) return true;
            var str = message.Substring(0, 1);

            // Get covalence player
            var iplayer = Covalence.PlayerManager.FindPlayerById(steamUserID.ToString());

            // Is it a chat command?
            if (!str.Equals("/"))
            {
                var chatSpecific = Interface.Call("OnPlayerChat", steamUserID.ToString(), message);
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

            // Get session IMyPlayer
            var session = Player.Find(steamUserID);
            // Handle it
            if (!cmdlib.HandleChatCommand(session, cmd, args))
            {
                Player.Reply(session, Lang("UnknownCommand", session.SteamUserId.ToString(), cmd));
                return true;
            }

            // Call the game hook
            Interface.Call("OnChatCommand", steamUserID.ToString(), command, args);

            return true;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="name"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(MyPlayer player)
        {
            IMyPlayer session = player as IMyPlayer;
            if (session == null) return;

            // Do permission stuff
            if (permission.IsLoaded)
            {
                var id = session.SteamUserId.ToString();

                // Update stored name
                permission.UpdateNickname(id, session.DisplayName);

                // Add player to default group
                if (!permission.UserHasGroup(id, DefaultGroups[0])) permission.AddUserGroup(id, DefaultGroups[0]);

                // Add player to admin group if admin
                if (session.PromoteLevel == MyPromoteLevel.Admin && !permission.UserHasGroup(id, DefaultGroups[2])) permission.AddUserGroup(id, DefaultGroups[2]);
            }

            // Call game hook
            Interface.Call("OnPlayerConnected", session);

            // Let covalence know
            Covalence.PlayerManager.NotifyPlayerConnect(session);
            var iplayer = Covalence.PlayerManager.FindPlayerById(session.SteamUserId.ToString());
            if (iplayer != null) Interface.Call("OnUserConnected", iplayer);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="session"></param>
        [HookMethod("OnPlayerDisconnected")]
        private void OnPlayerDisconnected(ulong steamId)
        {
            Interface.Oxide.ServerConsole.AddMessage($"OnPlayerDisconnected {steamId}", ConsoleColor.Yellow);
            IMyPlayer session = Player.Find(steamId.ToString());
            Interface.Oxide.ServerConsole.AddMessage($" *  {session}", ConsoleColor.Yellow);
            if (session == null) return;
            // Let covalence know
            var iplayer = Covalence.PlayerManager.FindPlayerById(steamId.ToString());
            if (iplayer != null) Interface.Call("OnUserDisconnected", iplayer, "Unknown");
            Covalence.PlayerManager.NotifyPlayerDisconnect(session);
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
            //if (Covalence.CommandSystem.HandleConsoleMessage(Covalence.CommandSystem.consolePlayer, arg)) return true;

            //return cmdlib.HandleConsoleCommand(command, args);
            return false;
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
        private bool IsAdmin(IMyPlayer session)
        {
            if (session == null || session.PromoteLevel == MyPromoteLevel.Admin) return true;
            Player.Reply(session, Lang("YouAreNotAdmin", session.SteamUserId.ToString()));
            return false;
        }

        /// <summary>
        /// Returns the localized message from key using optional ID string
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="args"></param>
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion


        //#region Console/Logging

        ///// <summary>
        ///// Prints an info message to the server console/log
        ///// </summary>
        ///// <param name="message"></param>
        //public void Print(string message) => MyLog.Default.Info(message);

        ///// <summary>
        ///// Prints a warning message to the server console/log
        ///// </summary>
        ///// <param name="message"></param>
        //public void PrintWarning(string message) => MyLog.Default.Warning(message);

        ///// <summary>
        ///// Prints an error message to the server console/log
        ///// </summary>
        ///// <param name="message"></param>
        //public void PrintError(string message) => MyLog.Default.Error(message);

        //#endregion
    }
}
