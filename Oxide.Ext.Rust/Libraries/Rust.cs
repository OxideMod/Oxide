using System;
using System.Collections.Generic;
using System.Linq;

using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using UnityEngine;

namespace Oxide.Rust.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for rust
    /// </summary>
    public class Rust : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Returns the UserID for the specified connection as a string
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromConnection")]
        public string UserIDFromConnection(Network.Connection connection)
        {
            return connection.userid.ToString();
        }

        /// <summary>
        /// Returns the UserID for the specified player as a string
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromPlayer")]
        public string UserIDFromPlayer(BasePlayer player)
        {
            return player.userID.ToString();
        }

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null)
        {
            if (message != null)
            {
                ConsoleSystem.Broadcast("chat.add " + name + " " + message.QuoteSafe());
            }
            else
            {
                message = name;
                ConsoleSystem.Broadcast("chat.add SERVER " + message.QuoteSafe());
            }
        }
    }
}
