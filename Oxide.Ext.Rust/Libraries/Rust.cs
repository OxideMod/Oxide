using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using UnityEngine;

namespace Oxide.Rust.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Rust
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
                ConsoleSystem.Broadcast("chat.add", userid, string.Format("<color=orange>{0}</color>  {1}", name, message), 1.0);
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
            if (message != null)
            {
                player.SendConsoleCommand("chat.add", userid, string.Format("<color=orange>{0}</color>  {1}", name, message), 1.0);
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
            player.transform.position = new UnityEngine.Vector3(x, y, z);
            player.ClientRPC(null, player, "ForcePositionTo", new object[] { player.transform.position });
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
