using System;
using System.Collections.Generic;
using System.Linq;
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
        public ChatCommandHandler(CommandCallback callback, Func<string, bool> commandFilter)
        {
            this.callback = callback;
            this.commandFilter = commandFilter;
        }

        /// <summary>
        /// Handles a chat message from the specified player, returns true if handled
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        public bool HandleChatMessage(ILivePlayer player, string str)
        {
            // Get the args
            if (str.Length == 0) return false;

            // Is it a chat command?
            if (str[0] == '/' || str[0] == '!')
            {
                // Get the message
                string message = str.Substring(1);

                // Parse it
                string cmd;
                string[] args;
                ParseChatCommand(message, out cmd, out args);
                if (cmd == null) return false;

                // Handle it
                return HandleChatCommand(player, cmd, args);
            }
            else
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

            // Handle
            return callback(cmd, CommandType.Chat, player?.BasePlayer, args);
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
                {
                    sb.Append(c);
                }
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

    }
}
