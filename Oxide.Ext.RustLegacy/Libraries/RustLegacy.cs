using System;
using System.Reflection;

using Oxide.Core.Libraries;

namespace Oxide.RustLegacy.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Rust
    /// </summary>
    public class RustLegacy : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Returns the UserID for the specified player as a string
        /// </summary>
        /// <param name="netuser"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromPlayer")]
        public string UserIDFromPlayer(NetUser netuser)
        {
            return netuser.userID.ToString();
        }

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string format, params object[] args)
        {
            foreach (var playerClient in PlayerClient.All)
                ConsoleNetworker.SendClientCommand(playerClient.netUser.networkPlayer, "chat.add Oxide " + QuoteSafe(string.Format(format, args)));
        }

        /// <summary>
        /// Sends a chat message to the user
        /// </summary>
        /// <param name="name"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(NetUser netUser, string format, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, "chat.add Oxide " + QuoteSafe(string.Format(format, args)));
        }

        /// <summary>
        /// Print a message to a players console log
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("SendConsoleMessage")]
        public void SendConsoleMessage(NetUser netUser, string format, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, "echo " + QuoteSafe(string.Format(format, args)));
        }

        /// <summary>
        /// Print a message to every players console log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("BroadcastConsole")]
        public void BroadcastConsole(string format, params object[] args)
        {
            foreach (var playerClient in PlayerClient.All)
                ConsoleNetworker.SendClientCommand(playerClient.netUser.networkPlayer, "echo " + QuoteSafe(string.Format(format, args)));
        }

        /// <summary>
        /// Gets private bindingflag for accessing private methods, fields, and properties
        /// </summary>
        [LibraryFunction("PrivateBindingFlag")]
        public BindingFlags PrivateBindingFlag()
        {
            return (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        }

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        [LibraryFunction("QuoteSafe")]
        public string QuoteSafe(string str)
        {
            str = str.Replace("\"", "\\\"");
            str = str.TrimEnd(new char[] { '\\' });
            return string.Concat("\"", str, "\"");
        }

        [LibraryFunction("FindPlayer")]
        public NetUser FindPlayer(string strNameOrIDOrIP)
        {
            NetUser netUser;
            if ((netUser = PlayerClient.All.Find((PlayerClient p) => p.netUser.userID.ToString() == strNameOrIDOrIP)?.netUser) != null)
            {
                return netUser;
            }
            if ((netUser = PlayerClient.All.Find((PlayerClient p) => p.netUser.displayName.StartsWith(strNameOrIDOrIP, StringComparison.CurrentCultureIgnoreCase))?.netUser) != null)
            {
                return netUser;
            }
            if ((netUser = PlayerClient.All.Find((PlayerClient p) => p.netUser.networkPlayer.ipAddress == strNameOrIDOrIP)?.netUser) != null)
            {
                return netUser;
            }
            return null;
        }
    }
}