using System;
using System.Collections.Generic;
using System.Text;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic chat command handler
    /// </summary>
    public sealed class ChatCommandHandler
    {
        // The Covalence command callback
        private CommandCallback callback;

        // The command filter
        private Func<string, bool> commandFilter;

        /// <summary>
        /// Initializes a new instance of the ChatCommandHandler class
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="commandFilter"></param>
        public ChatCommandHandler(CommandCallback callback, Func<string, bool> commandFilter)
        {
            this.callback = callback;
            this.commandFilter = commandFilter;
        }

        /// <summary>
        /// Handles a chat message from the specified player, returns true if handled
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        public bool HandleChatMessage(ILivePlayer player, string str)
        {
            // Get the args
            if (str.Length == 0) return false;

            // Is it a chat command?
            if (str[0] == '/' || str[0] == '!')
            {
                // Get the message
                var message = str.Substring(1);

                // Parse it
                string cmd;
                string[] args;
                ParseChatCommand(message, out cmd, out args);

                // Handle it
                return cmd != null && HandleChatCommand(player, cmd, args);
            }

            return false;
        }

        /// <summary>
        /// Handles a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private bool HandleChatCommand(ILivePlayer player, string cmd, string[] args)
        {
            // Check things
            if (commandFilter != null && !commandFilter(cmd)) return false;
            if (callback == null) return false;
            player.LastCommand = CommandType.Chat;

            // Handle it
            return callback(player.BasePlayer, cmd, args);
        }

        /// <summary>
        /// Handle console input from the specified player, returns true if handled
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        public bool HandleConsoleMessage(ILivePlayer player, string str)
        {
            // Parse it
            string cmd;
            string[] args;
            ParseChatCommand(str, out cmd, out args);

            // Handle it
            return cmd != null && HandleConsoleCommand(player, cmd, args);
        }

        /// <summary>
        /// Handles a console command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private bool HandleConsoleCommand(ILivePlayer player, string cmd, string[] args)
        {
            // Check things
            if (commandFilter != null && !commandFilter(cmd)) return false;
            if (callback == null) return false;
            player.LastCommand = CommandType.Console;

            // Handle it
            return callback(player.BasePlayer, cmd, args);
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
            for (var i = 0; i < argstr.Length; i++)
            {
                var c = argstr[i];
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
    }
}
