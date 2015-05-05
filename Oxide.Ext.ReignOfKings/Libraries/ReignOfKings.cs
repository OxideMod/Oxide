using System.Reflection;

using Oxide.Core.Libraries;

using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events;

namespace Oxide.ReignOfKings.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Reign of Kings
    /// </summary>
    public class ReignOfKings : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Returns the Id for the specified player as a string
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [LibraryFunction("IdFromPlayer")]
        public string IdFromPlayer(Player player)
        {
            return player.Id.ToString();
        }

        /// <summary>
        /// Returns the player for the specified Id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [LibraryFunction("PlayerFromId")]
        public Player PlayerFromId(string Id)
        {
            foreach(var player in Server.ClientPlayers)
            {
                if (player.Id.ToString() == Id)
                    return player;
            }

            return null;
        }

        /// <summary>
        /// Returns the SenderId of a NetworkEvent
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [LibraryFunction("GetEventSenderId")]
        public string GetEventSenderId(NetworkEvent e)
        {
            return e.SenderId.ToString();
        }

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <param name="userid"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null)
        {
            if (message != null)
                Server.BroadcastMessage(string.Format("{0}: {1}", name, message));
            else
            {
                message = name;
                Server.BroadcastMessage(message);
            }
        }

        /// <summary>
        /// Sends a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <param name="userid"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(Player player, string name, string message = null)
        {
            if (message != null)
                player.SendMessage(string.Format("{0}: {1}", name, message));
            else
            {
                message = name;
                player.SendMessage(message);
            }
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
            return "\"" + str.Replace("\"", "\\\"").TrimEnd('\\') + "\"";
        }
    }
}
