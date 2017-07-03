﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.TheForest.Libraries.Covalence;
using Steamworks;
using TheForest.Utils;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The core The Forest plugin
    /// </summary>
    public partial class TheForestCore : CSPlugin
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the TheForestCore class
        /// </summary>
        public TheForestCore()
        {
            // Set plugin info attributes
            Title = "The Forest";
            Author = "Oxide Team";
            var assemblyVersion = TheForestExtension.AssemblyVersion;
            Version = new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        }

        // Libraries
        //internal readonly Command cmdlib = Interface.Oxide.GetLibrary<Command>();
        internal readonly Lang lang = Interface.Oxide.GetLibrary<Lang>();
        internal readonly Permission permission = Interface.Oxide.GetLibrary<Permission>();
        //internal readonly Player Player = Interface.Oxide.GetLibrary<Player>();

        // Instances
        internal static readonly TheForestCovalenceProvider Covalence = TheForestCovalenceProvider.Instance;
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

        // TODO: Localization of core

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

            Analytics.Collect();
            TheForestExtension.ServerConsole();
            SteamGameServer.SetGameTags("oxide,modded");

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
        private object IOnServerCommand(BoltConnection connection, string command, string data) // TODO: Convert string data to string[] args?
        {
            if (command.Length == 0) return null;
            if (Interface.Call("OnServerCommand", command, data) != null) return true;

            // Check if command is from a player
            if (connection == null) return null;

            var id = connection.RemoteEndPoint.SteamId.Id.ToString();
            var entity = Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.source.ConnectionId == connection.ConnectionId);
            if (entity == null) return null;

            // Get the full command
            var message = command.TrimStart('/');

            // Parse it
            string cmd;
            string[] args;
            ParseCommand(message, out cmd, out args);
            if (cmd == null) return null;

            // Get the covalence player
            var iplayer = Covalence.PlayerManager.FindPlayerById(id);
            if (iplayer == null) return null;

            // Is the command blocked?
            var blockedSpecific = Interface.Call("OnPlayerCommand", entity, cmd, args);
            var blockedCovalence = Interface.Call("OnUserCommand", iplayer, cmd, args);
            if (blockedSpecific != null || blockedCovalence != null) return true;

            // Is it a chat command?
            if (message[0] != '/') return null;

            // Is it a covalance command?
            if (Covalence.CommandSystem.HandleChatMessage(iplayer, command)) return true;

            // Is it a regular chat command?
            //if (!cmdlib.HandleChatCommand(player, cmd, args)) // TODO: Implement
            //    iplayer.Reply(lang.GetMessage("UnknownCommand", this, iplayer.Id), cmd);

            return true;
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
