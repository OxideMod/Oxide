using System;
using System.Collections.Generic;
using System.Text;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic chat command handler
    /// </summary>
    public sealed class CommandHandler
    {
        // The Covalence command callback
        private CommandCallback callback;

        // The command filter
        private Func<string, bool> commandFilter;

        /// <summary>
        /// Initializes a new instance of the commandHandler class
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="commandFilter"></param>
        public CommandHandler(CommandCallback callback, Func<string, bool> commandFilter)
        {
            this.callback = callback;
            this.commandFilter = commandFilter;
        }

        /// <summary>
        /// Handles a chat message from the specified player, returns true if handled
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        public bool HandleChatMessage(IPlayer player, string message)
        {
            // Make sure the message is not empty
            if (message.Length == 0) return false;

            // Is it a chat command?
            if (message[0] != '/') return false;

            // Get the message
            message = message.Substring(1);

            // Parse the command
            string command;
            string[] args;
            ParseCommand(message, out command, out args);

            // Set command type for the player
            player.LastCommand = CommandType.Chat;

            // Handle the command
            return command != null && HandleCommand(player, command, args);
        }

        /// <summary>
        /// Handle console input from the specified player, returns true if handled
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        public bool HandleConsoleMessage(IPlayer player, string message)
        {
            // Handle global parent for console commands
            if (message.StartsWith("global.")) message = message.Substring(7);

            // Parse the command
            string command;
            string[] args;
            ParseCommand(message, out command, out args);

            // Set command type for the player
            player.LastCommand = CommandType.Console;

            // Handle the command
            return command != null && HandleCommand(player, command, args);
        }

        /// <summary>
        /// Handles a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private bool HandleCommand(IPlayer player, string command, string[] args)
        {
            // Handle the command
            return (commandFilter == null || commandFilter(command)) && callback != null && callback(player, command, args);
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
            cmd = arglist[0].ToLowerInvariant();
            arglist.RemoveAt(0);
            args = arglist.ToArray();
        }
    }
}
