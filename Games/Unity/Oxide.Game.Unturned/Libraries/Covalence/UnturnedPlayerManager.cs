using System;
using System.Collections.Generic;
using System.Linq;

using ProtoBuf;
using SDG.Unturned;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Unturned.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class UnturnedPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public ulong Id;
        }

        private readonly IDictionary<string, PlayerRecord> playerData;
        private readonly IDictionary<string, UnturnedPlayer> allPlayers;
        private readonly IDictionary<string, UnturnedPlayer> connectedPlayers;

        internal UnturnedPlayerManager()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence") ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, UnturnedPlayer>();
            foreach (var pair in playerData) allPlayers.Add(pair.Key, new UnturnedPlayer(pair.Value.Id, pair.Value.Name));
            connectedPlayers = new Dictionary<string, UnturnedPlayer>();
        }

        private void NotifyPlayerJoin(SteamPlayer steamPlayer)
        {
            var id = steamPlayer.playerID.steamID.ToString();

            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(id, out record))
            {
                // Update
                record.Name = steamPlayer.player.name;
                playerData[id] = record;

                // Swap out Rust player
                allPlayers.Remove(id);
                allPlayers.Add(id, new UnturnedPlayer(steamPlayer));
            }
            else
            {
                // Insert
                record = new PlayerRecord {Id = steamPlayer.playerID.steamID.m_SteamID, Name = steamPlayer.player.name};
                playerData.Add(id, record);

                // Create Rust player
                allPlayers.Add(id, new UnturnedPlayer(steamPlayer));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence");
        }

        internal void NotifyPlayerConnect(SteamPlayer steamPlayer)
        {
            var id = steamPlayer.playerID.steamID;
            connectedPlayers[id.ToString()] = new UnturnedPlayer(steamPlayer);
        }

        internal void NotifyPlayerDisconnect(SteamPlayer steamPlayer) => connectedPlayers.Remove(steamPlayer.playerID.steamID.ToString());

        #region Player Finding

        /// <summary>
        /// Gets a player using their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string id)
        {
            UnturnedPlayer player;
            return allPlayers.TryGetValue(id, out player) ? player : null;
        }

        /// <summary>
        /// Gets a player using their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer this[int id]
        {
            get
            {
                UnturnedPlayer player;
                return allPlayers.TryGetValue(id.ToString(), out player) ? player : null;
            }
        }

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => allPlayers.Values.Cast<IPlayer>();
        public IEnumerable<IPlayer> GetAllPlayers() => allPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all connected allPlayers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Connected => connectedPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Finds a single player given a partial name (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialNameOrId)
        {
            var players = FindPlayers(partialNameOrId).ToList();
            return players.Count == 1 ? players.Single() : null;
        }

        /// <summary>
        /// Finds any number of players given a partial name (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialNameOrId)
        {
            return allPlayers.Values.Where(p => p.Name.ToLower().Contains(partialNameOrId.ToLower()) || p.Id == partialNameOrId).Cast<IPlayer>();
        }

        /// <summary>
        /// Gets a connected player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer GetConnectedPlayer(string id)
        {
            UnturnedPlayer player;
            return connectedPlayers.TryGetValue(id, out player) ? player : null;
        }

        /// <summary>
        /// Finds a single connected player given a partial name (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IPlayer FindConnectedPlayer(string partialNameOrId) => FindConnectedPlayers(partialNameOrId).FirstOrDefault();

        /// <summary>
        /// Finds any number of connected players given a partial name (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindConnectedPlayers(string partialNameOrId)
        {
            return connectedPlayers.Values .Where(p => p.Name.IndexOf(partialNameOrId,  StringComparison.OrdinalIgnoreCase) >= 0).Cast<IPlayer>();
        }

        #endregion
    }
}
