using System;
using System.Collections.Generic;
using System.Linq;

using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

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
        [LibraryFunction("UserIDsFromBuildingPrivilege")]
        public Array UserIDsFromBuildingPrivilege(BuildingPrivlidge buildingpriv)
        {
            List<string> list = new List<string>();
            foreach (ProtoBuf.PlayerNameID eid in buildingpriv.authorizedPlayers)
            {
                list.Add(eid.userid.ToString());
            }
            return list.ToArray();
        }
        [LibraryFunction("UserIDFromDeployedItem")]
        public string UserIDFromDeployedItem(DeployedItem DeployedItem)
        {
            return DeployedItem.deployerUserID.ToString();
        }
    }
}
