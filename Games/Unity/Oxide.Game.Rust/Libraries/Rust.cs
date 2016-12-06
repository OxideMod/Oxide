using System;
using System.Linq;
using System.Reflection;
using Network;
using Oxide.Core;
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

        #endregion

        #region Chat

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <param name="userId"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null, string userId = "0")
        {
            ConsoleNetwork.BroadcastToAllClients("chat.add", userId, message != null ? $"<color=orange>{name}</color>: {message}" : name, 1.0);
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <param name="userId"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(BasePlayer player, string name, string message = null, string userId = "0")
        {
            player?.SendConsoleCommand("chat.add", userId, message != null ? $"<color=orange>{name}</color>: {message}" : name, 1.0);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Runs a client command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [LibraryFunction("RunClientCommand")]
        public void RunClientCommand(BasePlayer player, string command, params object[] args) => player.SendConsoleCommand(command, args);

        /// <summary>
        /// Runs a server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [LibraryFunction("RunServerCommand")]
        public void RunServerCommand(string command, params object[] args) => ConsoleSystem.Run.Server.Normal(command, args);

        #endregion

        /// <summary>
        /// Returns the Steam ID for the specified connection as a string
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromConnection")]
        public string UserIDFromConnection(Connection connection) => connection.userid.ToString();

        /// <summary>
        /// Returns the Steam ID for the specified building privilege as an array
        /// </summary>
        /// <param name="priv"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDsFromBuildingPrivilege")]
        public Array UserIDsFromBuildingPrivlidge(BuildingPrivlidge priv) => priv.authorizedPlayers.Select(eid => eid.userid.ToString()).ToArray();

        /// <summary>
        /// Returns the Steam ID for the specified player as a string
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromPlayer")]
        public string UserIDFromPlayer(BasePlayer player) => player.UserIDString;

        /// <summary>
        /// Returns the Steam ID for the specified entity as a string
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        [LibraryFunction("OwnerIDFromEntity")]
        public string OwnerIDFromEntity(BaseEntity entity) => entity.OwnerID.ToString();

        /// <summary>
        /// Returns the player for the specified name, id or ip
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        [LibraryFunction("FindPlayer")]
        public BasePlayer FindPlayer(string nameOrIdOrIp) => RustCore.FindPlayer(nameOrIdOrIp);

        /// <summary>
        /// Returns the player for the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("FindPlayerByName")]
        public BasePlayer FindPlayerByName(string name) => RustCore.FindPlayerByName(name);

        /// <summary>
        /// Returns the player for the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [LibraryFunction("FindPlayerById")]
        public BasePlayer FindPlayerById(ulong id) => RustCore.FindPlayerById(id);

        /// <summary>
        /// Returns the player for the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [LibraryFunction("FindPlayerByIdString")]
        public BasePlayer FindPlayerByIdString(string id) => RustCore.FindPlayerByIdString(id);

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
            player.MovePosition(player.transform.position);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
            player.TransformChanged();
        }
    }
}
