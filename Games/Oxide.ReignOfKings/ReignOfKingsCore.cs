using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.ReignOfKings.Libraries;
using Oxide.Game.ReignOfKings.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

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
            Author = ReignOfKingsExtension.AssemblyAuthors;
            Version = ReignOfKingsExtension.AssemblyVersion;

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

        #endregion Initialization

        #region Core Hooks

        [HookMethod("Init")]
        private void Init()
        {
            // Configure remote error logging
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

        #endregion Core Hooks

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

        [HookMethod("IOnServerCommand")]
        private object IOnServerCommand(ulong id, string str)
        {
            if (str.Length == 0) return null;
            if (Interface.Call("OnServerCommand", str) != null) return true;

            // Check if command is from the player
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

        #endregion Command Handling

        #region Helpers

        /// <summary>
        /// Checks if the permission system has loaded, shows an error if it failed to load
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private bool PermissionsLoaded(IPlayer player)
        {
            if (permission.IsLoaded) return true;
            player.Reply(string.Format(lang.GetMessage("PermissionsNotLoaded", this, player.Id), permission.LastException.Message));
            return false;
        }

        #endregion Helpers
    }
}
