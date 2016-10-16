using System.Collections.Generic;
using System.Linq;

using ProtoBuf;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class RustPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public ulong Id;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, RustPlayer> allPlayers;
        private IDictionary<string, RustPlayer> connectedPlayers;

        internal void Initialize()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence") ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, RustPlayer>();
            foreach (var pair in playerData) allPlayers.Add(pair.Key, new RustPlayer(pair.Value.Id, pair.Value.Name));
            connectedPlayers = new Dictionary<string, RustPlayer>();
        }

        private void NotifyPlayerJoin(BasePlayer player)
        {
            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(player.UserIDString, out record))
            {
                // Update
                record.Name = player.displayName;
                playerData[player.UserIDString] = record;

                // Swap out Rust player
                allPlayers.Remove(player.UserIDString);
                allPlayers.Add(player.UserIDString, new RustPlayer(player));
            }
            else
            {
                // Insert
                record = new PlayerRecord { Id = player.userID, Name = player.displayName };
                playerData.Add(player.UserIDString, record);

                // Create Rust player
                allPlayers.Add(player.UserIDString, new RustPlayer(player));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence");
        }

        internal void NotifyPlayerConnect(BasePlayer player)
        {
            NotifyPlayerJoin(player);
            connectedPlayers[player.UserIDString] = new RustPlayer(player);
        }

        internal void NotifyPlayerDisconnect(BasePlayer player) => connectedPlayers.Remove(player.UserIDString);

        #region Player Finding

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => allPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Connected => connectedPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Finds a single player given a partial name or unique ID (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialNameOrId)
        {
            var players = FindPlayers(partialNameOrId).ToList();
            return players.Count == 1 ? players.Single() : null;
        }

        /// <summary>
        /// Finds any number of players given a partial name or unique ID (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
        {
            return allPlayers.Values.Where(p => (p.Name != null && p.Name.ToLower().Contains(partialNameOrId.ToLower())) || p.Id == partialNameOrId).Cast<IPlayer>();
        }

        #endregion
    }
}
