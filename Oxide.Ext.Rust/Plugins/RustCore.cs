using System;
using System.Text;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

using Oxide.Rust.Libraries;

namespace Oxide.Rust.Plugins
{
    /// <summary>
    /// The core rust plugin
    /// </summary>
    public class RustCore : CSPlugin
    {
        // The logger
        private Logger logger;

        // The pluginmanager
        private PluginManager pluginmanager;

        /// <summary>
        /// Initialises a new instance of the RustCore class
        /// </summary>
        public RustCore()
        {
            // Set attributes
            Name = "rustcore";
            Title = "Rust Core";
            Author = "Oxide Team";
            Version = new VersionNumber(1, 0, 0);
            HasConfig = false;

            // Get logger
            logger = Interface.GetMod().RootLogger;
            
            // Get the pluginmanager
            pluginmanager = Interface.GetMod().RootPluginManager;
        }

        /// <summary>
        /// Loads the default config for this plugin
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            // No config yet, we might use it later
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when the plugin is initialising
        /// </summary>
        [HookMethod("Init")]
        private void Init()
        {
            // Get the command library
            Command cmdlib = Interface.GetMod().GetLibrary<Command>("Command");

            // Add our commands
            cmdlib.AddConsoleCommand("oxide.load", this, "cmdLoad");
            cmdlib.AddConsoleCommand("oxide.unload", this, "cmdUnload");
            cmdlib.AddConsoleCommand("oxide.reload", this, "cmdReload");
        }



        /// <summary>
        /// Called when the "oxide.load" command has been executed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        [HookMethod("cmdLoad")]
        private void cmdLoad(ConsoleSystem.Arg arg)
        {
            // Check arg 1 exists
            if (!arg.HasArgs(1))
            {
                arg.ReplyWith("Syntax: oxide.load <pluginname>");
                return;
            }

            // Get the plugin name
            string name = arg.GetString(0);
            if (string.IsNullOrEmpty(name)) return;

            // Load
            Interface.GetMod().LoadPlugin(name);
            Plugin plugin = pluginmanager.GetPlugin(name);
            plugin.CallHook("OnServerInitialized", null);
        }

        /// <summary>
        /// Called when the "oxide.unload" command has been executed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        [HookMethod("cmdUnload")]
        private void cmdUnload(ConsoleSystem.Arg arg)
        {
            // Check arg 1 exists
            if (!arg.HasArgs(1))
            {
                arg.ReplyWith("Syntax: oxide.unload <pluginname>");
                return;
            }

            // Get the plugin name
            string name = arg.GetString(0);
            if (string.IsNullOrEmpty(name)) return;

            // Unload
            if (!Interface.GetMod().UnloadPlugin(name))
            {
                arg.ReplyWith(string.Format("Plugin '{0}' not found!", name));
            }
            else
            {
                arg.ReplyWith(string.Format("Plugin '{0}' unloaded.", name));
            }
        }

        /// Called when the "oxide.reload" command has been executed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        [HookMethod("cmdReload")]
        private void cmdReload(ConsoleSystem.Arg arg)
        {
            // Check arg 1 exists
            if (!arg.HasArgs(1))
            {
                arg.ReplyWith("Syntax: oxide.reload <pluginname>");
                return;
            }

            // Get the plugin name
            string name = arg.GetString(0);
            if (string.IsNullOrEmpty(name)) return;

            // Reload
            if (!Interface.GetMod().ReloadPlugin(name))
            {
                arg.ReplyWith(string.Format("Plugin '{0}' not found!", name));
            }
        }

        /// <summary>
        /// Called when the server wants to know what tags to use
        /// </summary>
        /// <param name="oldtags"></param>
        /// <returns></returns>
        [HookMethod("ModifyTags")]
        private string ModifyTags(string oldtags)
        {
            // We're going to call out and build a list of all tags to use
            List<string> taglist = new List<string>(oldtags.Split(','));
            Interface.CallHook("BuildServerTags", new object[] { taglist });
            string newtags = string.Join(",", taglist.ToArray());
            return newtags;
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
        /// Called when a user is attempting to connect
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [HookMethod("OnUserApprove")]
        private object OnUserApprove(Network.Connection connection)
        {
            // Call out and see if we should reject
            object canlogin = Interface.CallHook("CanClientLogin", new object[] { connection });
            if (canlogin != null)
            {
                // If it's a bool and it's true, let them in
                if (canlogin is bool && (bool)canlogin) return null;

                // If it's a string, reject them with a message
                if (canlogin is string)
                {
                    ConnectionAuth.Reject(connection, (string)canlogin);
                    return true;
                }

                // We don't know what type it is, reject them with it anyway
                ConnectionAuth.Reject(connection, canlogin.ToString());
                return true;
            }
            return null;
        }

        /// <summary>
        /// Called when a console command was run
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="wantsfeedback"></param>
        /// <returns></returns>
        [HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg, bool wantsfeedback)
        {
            // Sanity checks
            if (arg == null) return null;
            if (arg.cmd == null) return null;

            // Is it chat.say?
            if (arg.cmd.namefull == "chat.say")
            {
                // Get the args
                string str = arg.GetString(0, "text");
                if (str.Length == 0) return null;

                // Is it a chat command?
                if (str[0] == '/' || str[0] == '!')
                {
                    // Get the arg string
                    string argstr = str.Substring(1);

                    // Parse it
                    string cmd;
                    string[] args;
                    ParseChatCommand(argstr, out cmd, out args);
                    if (cmd == null) return null;

                    // Handle it
                    Command cmdlib = Interface.GetMod().GetLibrary<Command>("Command");
                    BasePlayer ply = arg.connection.player as BasePlayer;
                    if (ply == null)
                    {
                        Interface.GetMod().RootLogger.Write(LogType.Debug, "Player is actually a {0}!", arg.connection.player.GetType());
                    }
                    else
                    {
                        if (!cmdlib.HandleChatCommand(ply, cmd, args))
                        {
                            ply.SendConsoleCommand(string.Format("chat.add \"SERVER\" \"Unknown command '{0}'!\"", cmd));
                        }
                    }

                    // Handled
                    arg.ReplyWith(string.Empty);
                    return true;
                }
            }

            // Default behaviour
            return null;
        }

        /// <summary>
        /// Parses the specified chat command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private void ParseChatCommand(string argstr, out string cmd, out string[] args)
        {
            List<string> arglist = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inlongarg = false;
            for (int i = 0; i < argstr.Length; i++)
            {
                char c = argstr[i];
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        string arg = sb.ToString().Trim();
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
                    string arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg)) arglist.Add(arg);
                    sb = new StringBuilder();
                }
                else
                    sb.Append(c);
            }
            if (sb.Length > 0)
            {
                string arg = sb.ToString().Trim();
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
        /// Called when the player has been initialised
        /// </summary>
        /// <param name="player"></param>
        [HookMethod("OnPlayerInit")]
        private void OnPlayerInit(BasePlayer player)
        {

        }


        
    }
}
