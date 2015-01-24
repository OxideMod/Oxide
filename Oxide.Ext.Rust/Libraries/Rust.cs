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
        /// Returns the UserIDs for the specified building privilege as an array
        /// </summary>
        /// <param name="privilege"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDsFromBuildingPrivilege")]
        public Array UserIDsFromBuildingPrivlidge(BuildingPrivlidge buildingpriv)
        {
            List<string> list = new List<string>();
            foreach (ProtoBuf.PlayerNameID eid in buildingpriv.authorizedPlayers)
            {
                list.Add(eid.userid.ToString());
            }
            return list.ToArray();
        }

        /// <summary>
        /// Returns the UserID for the specified deployed item as a string
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromDeployedItem")]
        public string UserIDFromDeployedItem(DeployedItem DeployedItem)
        {
            return DeployedItem.deployerUserID.ToString();
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
                ConsoleSystem.Broadcast("chat.add " + name.QuoteSafe() + " " + message.QuoteSafe() + " 1.0");
            }
            else
            {
                message = name;
                ConsoleSystem.Broadcast("chat.add \"SERVER\" " + message.QuoteSafe() + " 1.0");
            }
        }

        /// <summary>
        /// Sends a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(BasePlayer player, string name, string message = null)
        {
            if (message != null)
            {
                player.SendConsoleCommand("chat.add " + name.QuoteSafe() + " " + message.QuoteSafe() + " 1.0");
            }
            else
            {
                message = name;
                player.SendConsoleCommand("chat.add \"SERVER\" " + message.QuoteSafe() + " 1.0");
            }
        }
        
        /// <summary>
        /// Force client to teleport to position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        [LibraryFunction("ForcePlayerPosition")]
        public void ForcePlayerPosition(BasePlayer player, float x, float y, float z)
        {
            var position = player.transform.position;
            position.x = x;
            position.y = y;
            position.z = z;
            player.ClientRPC(null, player, "ForcePositionTo", new object[] { position });
            player.TransformChanged();
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
