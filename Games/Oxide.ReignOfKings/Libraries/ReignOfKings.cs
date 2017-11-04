using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events;
using Oxide.Core;
using Oxide.Core.Libraries;
using System.Linq;
using System.Reflection;

namespace Oxide.Game.ReignOfKings.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Reign of Kings
    /// </summary>
    public class ReignOfKings : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        /// <returns></returns>
        public override bool IsGlobal => false;

        #region Utility

        /// <summary>
        /// Gets private bindingflag for accessing private methods, fields, and properties
        /// </summary>
        [LibraryFunction("PrivateBindingFlag")]
        public BindingFlags PrivateBindingFlag() => (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        [LibraryFunction("QuoteSafe")]
        public string QuoteSafe(string str) => str.Quote();

        #endregion Utility

        #region Chat

        /// <summary>
        /// Broadcasts the specified chat message to all players
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null) => Server.BroadcastMessage(message != null ? $"{name}: {message}" : name);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(Player player, string name, string message = null) => player.SendMessage(message != null ? $"{name}: {message}" : name);

        #endregion Chat

        /// <summary>
        /// Returns the ID for the specified player as a string
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [LibraryFunction("IdFromPlayer")]
        public string IdFromPlayer(Player player) => player.Id.ToString();

        /// <summary>
        /// Returns the player for the specified ID
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        [LibraryFunction("PlayerFromId")]
        public Player PlayerFromId(string steamId) => Server.ClientPlayers.FirstOrDefault(player => player.Id.ToString() == steamId);

        /// <summary>
        /// Returns the SenderId of the NetworkEvent
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [LibraryFunction("GetEventSenderId")]
        public string GetEventSenderId(NetworkEvent e) => e.SenderId.ToString();
    }
}
