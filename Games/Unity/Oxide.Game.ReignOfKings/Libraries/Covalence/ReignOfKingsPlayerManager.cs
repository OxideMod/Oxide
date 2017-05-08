using System;
using System.Collections.Generic;
using System.Linq;
using CodeHatch.Engine.Networking;
using ProtoBuf;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class ReignOfKingsPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public ulong Id;
        }

        private readonly IDictionary<string, PlayerRecord> playerData;
        private readonly IDictionary<string, ReignOfKingsPlayer> allPlayers;
        private readonly IDictionary<string, ReignOfKingsPlayer> connectedPlayers;

        internal ReignOfKingsPlayerManager()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence") ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, ReignOfKingsPlayer>();
            foreach (var pair in playerData) allPlayers.Add(pair.Key, new ReignOfKingsPlayer(pair.Value.Id, pair.Value.Name));
            connectedPlayers = new Dictionary<string, ReignOfKingsPlayer>();

            // Cleanup old .data
            Cleanup.Add(ProtoStorage.GetFileDataPath("oxide.covalence.playerdata.data"));
        }

        private void NotifyPlayerJoin(Player player)
        {
            var id = player.Id.ToString();

            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(id, out record))
            {
                // Update
                record.Name = player.Name;
                playerData[id] = record;

                // Swap out Rust player
                allPlayers.Remove(id);
                allPlayers.Add(id, new ReignOfKingsPlayer(player));
            }
            else
            {
                // Insert
                record = new PlayerRecord { Id = player.Id, Name = player.Name };
                playerData.Add(id, record);

                // Create Rust player
                allPlayers.Add(id, new ReignOfKingsPlayer(player));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence");
        }

        internal void NotifyPlayerConnect(Player player)
        {
            NotifyPlayerJoin(player);
            connectedPlayers[player.Id.ToString()] = new ReignOfKingsPlayer(player);
        }

        internal void NotifyPlayerDisconnect(Player player) => connectedPlayers.Remove(player.Id.ToString());

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
            ReignOfKingsPlayer player;
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
