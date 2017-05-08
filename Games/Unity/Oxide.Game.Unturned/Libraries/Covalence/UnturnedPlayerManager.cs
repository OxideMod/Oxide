using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using ProtoBuf;
using SDG.Unturned;

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
                record = new PlayerRecord { Id = steamPlayer.playerID.steamID.m_SteamID, Name = steamPlayer.player.name };
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
            UnturnedPlayer player;
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
