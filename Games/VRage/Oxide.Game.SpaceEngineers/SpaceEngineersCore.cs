using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.SpaceEngineers.Libraries;
using Oxide.Game.SpaceEngineers.Libraries.Covalence;
using Sandbox;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using VRage.Game.ModAPI;

namespace Oxide.Game.SpaceEngineers
{
    /// <summary>
    /// The core Space Engineers plugin
    /// </summary>
    public partial class SpaceEngineersCore : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the SpaceEngineersCore class
        /// </summary>
        public SpaceEngineersCore()
        {
            // Set plugin info attributes
            Title = "Space Engineers";
            Author = SpaceEngineersExtension.AssemblyAuthors;
            Version = SpaceEngineersExtension.AssemblyVersion;
        }

        // Libraries
        internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly SpaceEngineersCovalenceProvider Covalence = SpaceEngineersCovalenceProvider.Instance;
        internal readonly PluginManager pluginManager = Interface.Oxide.RootPluginManager;
        internal readonly IServer Server = Covalence.CreateServer();

        // Commands that a plugin can't override
        internal static IEnumerable<string> RestrictedCommands => new[]
        {
            ""
        };

        public static string[] Filter =
        {
        };

        // Track 'load' chat commands
        private readonly List<string> loadingPlugins = new List<string>();

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

        //private SpaceEngineersLogger logger;

        private int m_totalTimeInMilliseconds;

        /// <summary>
        /// Starts the logging
        /// </summary>
        private void InitializeLogging()
        {
            Interface.Oxide.NextTick(() =>
            {
                //logger = new SpaceEngineersLogger();
                //Interface.Oxide.RootLogger.AddLogger(logger);
                Interface.Oxide.RootLogger.DisableCache();
            });
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

            /*SteamServerAPI.Instance.GameServer.SetGameTags(
                $"groupId{m_groupId} version{Server.Version" +
                $" datahash{MyDataIntegrityChecker.GetHashBase64()} {MyMultiplayer.ModCountTag}{ModCount}" +
                $" gamemode{gamemode} {MyMultiplayer.ViewDistanceTag}{ViewDistance}"
            );*/

            Analytics.Collect();
            SpaceEngineersExtension.ServerConsole();

            serverInitialized = true;
        }

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        [HookMethod("OnServerShutdown")]
        private void OnServerShutdown() => Interface.Oxide.OnShutdown();

        /// <summary>
        /// Called when the server is created sandbox
        /// </summary>
        [HookMethod("IOnSandboxCreated")]
        private void IOnSandboxCreated()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Utilities == null || MyAPIGateway.Multiplayer == null) return;

            MyAPIGateway.Multiplayer.RegisterMessageHandler(0xff20, OnChatMessageFromClient);
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
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        private void OnChatMessageFromClient(byte[] bytes)
        {
            var data = Encoding.Unicode.GetString(bytes);
            var args = data.Split(new char[] { ',' }, 2);
            ulong steamId = 0;
            if (ulong.TryParse(args[0], out steamId))
                IOnPlayerChat(steamId, args[1], ChatEntryTypeEnum.ChatMsg);
            else
                Interface.Oxide.LogError("Can't parse steam id...");
        }

        /// <summary>
        /// Called when the server logs append
        /// </summary>
        [HookMethod("OnWriteLine")]
        private void OnWriteLine(string message)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

            var color = ConsoleColor.Gray;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="steamId"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(ulong steamId, string message, ChatEntryTypeEnum chatType)
        {
            if (Sync.MyId == steamId || message.Trim().Length <= 1) return true;

            var str = message.Substring(0, 1);

            // Get covalence player
            var iplayer = Covalence.PlayerManager.FindPlayerById(steamId.ToString());
            var id = steamId.ToString();

            // Is it a chat command?
            if (!str.Equals("/"))
            {
                var chatSpecific = Interface.Call("OnPlayerChat", id, message);
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
            ParseCommand(command, out cmd, out args);
            if (cmd == null) return null;

            // Get session IMyPlayer
            var player = Player.Find(id);
            if (!cmdlib.HandleChatCommand(player, cmd, args))
            {
                Player.Reply(player, Lang("UnknownCommand", player.SteamUserId.ToString(), cmd));
                return true;
            }

            // Call the game hook
            Interface.Call("OnChatCommand", id, command, args);

            return true;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="myPlayer"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(MyPlayer myPlayer)
        {
            var player = myPlayer as IMyPlayer;
            if (player == null) return;

            var id = player.SteamUserId.ToString();

            // Update player's permissions group and name
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(id, player.DisplayName);
                var defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(id, defaultGroups.Players)) permission.AddUserGroup(id, defaultGroups.Players);
                if (player.PromoteLevel == MyPromoteLevel.Admin && !permission.UserHasGroup(id, defaultGroups.Administrators))
                    permission.AddUserGroup(id, defaultGroups.Administrators);
            }

            Interface.Call("OnPlayerConnected", player);

            Covalence.PlayerManager.PlayerJoin(player.SteamUserId, player.DisplayName); // TODO: Move to OnUserApprove hook once available
            Covalence.PlayerManager.PlayerConnected(player);
            var iplayer = Covalence.PlayerManager.FindPlayerById(id);
            if (iplayer != null) Interface.Call("OnUserConnected", iplayer);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="steamId"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(ulong steamId)
        {
            var player = Player.FindById(steamId);
            if (player == null) return;

            Interface.Call("OnPlayerDisconnected", player);

            var iplayer = Covalence.PlayerManager.FindPlayerById(steamId.ToString());
            if (iplayer != null) Interface.Call("OnUserDisconnected", iplayer, "Unknown");
            Covalence.PlayerManager.PlayerDisconnected(player);

            Interface.Oxide.ServerConsole.AddMessage($" *  {player}", ConsoleColor.Yellow);
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

        #endregion

        #region Helpers

        /// <summary>
        /// Returns if specified player is admin
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private bool IsAdmin(IMyPlayer player)
        {
            if (player == null || player.PromoteLevel == MyPromoteLevel.Admin) return true;
            Player.Reply(player, Lang("YouAreNotAdmin", player.SteamUserId.ToString()));
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
    }
}
