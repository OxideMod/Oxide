using System;
using System.Linq;
using System.Reflection;

using Network;

using Oxide.Core.Libraries;

using UnityEngine;

namespace Oxide.Game.Rust.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Rust
    /// </summary>
    public class Rust : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Returns the UserID for the specified connection as a string
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromConnection")]
        public string UserIDFromConnection(Connection connection)
        {
            return connection.userid.ToString();
        }

        /// <summary>
        /// Returns the UserIDs for the specified building privilege as an array
        /// </summary>
        /// <param name="buildingpriv"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDsFromBuildingPrivilege")]
        public Array UserIDsFromBuildingPrivlidge(BuildingPrivlidge buildingpriv)
        {
            return buildingpriv.authorizedPlayers.Select(eid => eid.userid.ToString()).ToArray();
        }

        /// <summary>
        /// Returns the UserID for the specified player as a string
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromPlayer")]
        public string UserIDFromPlayer(BasePlayer player)
        {
            return player.userID.ToString();
        }

        /// <summary>
        /// Runs a server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [LibraryFunction("RunServerCommand")]
        public void RunServerCommand(string command, params object[] args)
        {
            ConsoleSystem.Run.Server.Normal(command, args);
        }

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <param name="userid"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null, string userid = "0")
        {
            if (message != null)
            {
                ConsoleSystem.Broadcast("chat.add", userid, $"<color=orange>{name}</color>: {message}", 1.0);
            }
            else
            {
                message = name;
                ConsoleSystem.Broadcast("chat.add", userid, message, 1.0);
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
        public void SendChatMessage(BasePlayer player, string name, string message = null, string userid = "0")
        {
            if (player?.net == null) return;
            if (message != null)
            {
                player.SendConsoleCommand("chat.add", userid, $"<color=orange>{name}</color>: {message}", 1.0);
            }
            else
            {
                message = name;
                player.SendConsoleCommand("chat.add", userid, message, 1.0);
            }
        }

        /// <summary>
        /// Forces player position (teleportation)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        [LibraryFunction("ForcePlayerPosition")]
        public void ForcePlayerPosition(BasePlayer player, float x, float y, float z)
        {
            player.transform.position = new Vector3(x, y, z);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.TransformChanged();
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
            return str.QuoteSafe();
        }
    }
}
