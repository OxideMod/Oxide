using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using ProtoBuf;

namespace Oxide.Game.SevenDays.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class SevenDaysPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public string Id;
        }

        private readonly IDictionary<string, PlayerRecord> playerData;
        private readonly IDictionary<string, SevenDaysPlayer> allPlayers;
        private readonly IDictionary<string, SevenDaysPlayer> connectedPlayers;

        internal SevenDaysPlayerManager()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence") ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, SevenDaysPlayer>();
            foreach (var pair in playerData) allPlayers.Add(pair.Key, new SevenDaysPlayer(pair.Value.Id, pair.Value.Name));
            connectedPlayers = new Dictionary<string, SevenDaysPlayer>();
        }

        private void NotifyPlayerJoin(ClientInfo client)
        {
            var id = client.playerId;

            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(id, out record))
            {
                // Update
                record.Name = client.playerName;
                playerData[id] = record;

                // Swap out Rust player
                allPlayers.Remove(id);
                allPlayers.Add(id, new SevenDaysPlayer(client));
            }
            else
            {
                // Insert
                record = new PlayerRecord { Id = id, Name = client.playerName };
                playerData.Add(id, record);

                // Create Rust player
                allPlayers.Add(id, new SevenDaysPlayer(client));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence");
        }

        internal void NotifyPlayerConnect(ClientInfo client)
        {
            NotifyPlayerJoin(client);
            connectedPlayers[client.playerId] = new SevenDaysPlayer(client);
        }

        internal void NotifyPlayerDisconnect(ClientInfo client) => connectedPlayers.Remove(client.playerId);

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
        /// Finds a single player given unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer FindPlayerById(string id)
        {
            SevenDaysPlayer player;
            return allPlayers.TryGetValue(id, out player) ? player : null;
        }

        /// <summary>
        /// Finds a single connected player given game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IPlayer FindPlayerByObj(object obj) => connectedPlayers.Values.FirstOrDefault(p => p.Object == obj);

        /// <summary>
        /// Finds a single player given a partial name or unique ID (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialNameOrId)
        {
            var players = FindPlayers(partialNameOrId).ToArray();
            return players.Length == 1 ? players[0] : null;
        }

        /// <summary>
        /// Finds any number of players given a partial name or unique ID (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
        {
            foreach (var player in allPlayers.Values)
            {
                if (player.Name != null && player.Name.IndexOf(partialNameOrId, StringComparison.OrdinalIgnoreCase) >= 0 || player.Id == partialNameOrId)
                    yield return player;
            }
        }

        #endregion
    }
}
