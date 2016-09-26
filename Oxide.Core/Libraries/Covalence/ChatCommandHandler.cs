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
        public bool HandleChatMessage(IPlayer player, string str)
        {
            // Make sure the message is not empty
            if (str.Length == 0) return false;

            // Is it a chat command?
            if (str[0] != '/') return false;

            // Get the message
            var message = str.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseCommand(message, out cmd, out args);

            // Set command type for the player
            player.LastCommand = CommandType.Chat;

            // Handle it
            return cmd != null && HandleCommand(player, cmd, args);
        }

        /// <summary>
        /// Handle console input from the specified player, returns true if handled
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        public bool HandleConsoleMessage(IPlayer player, string str)
        {
            // Handle global parent for console commands
            if (str.StartsWith("global.")) str = str.Substring(7);

            // Parse it
            string cmd;
            string[] args;
            ParseCommand(str, out cmd, out args);

            // Set command type for the player
            player.LastCommand = CommandType.Console;

            // Handle it
            return cmd != null && HandleCommand(player, cmd, args);
        }

        /// <summary>
        /// Handles a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        private bool HandleCommand(IPlayer player, string cmd, string[] args)
        {
            // Check things
            if (commandFilter != null && !commandFilter(cmd)) return false;
            if (callback == null) return false;
            
            // Handle it
            return callback(player, cmd, args);
        }

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
