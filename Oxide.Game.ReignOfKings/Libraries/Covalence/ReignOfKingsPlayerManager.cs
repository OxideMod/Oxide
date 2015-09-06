using System;
using System.Collections.Generic;
using System.Linq;

using CodeHatch.Engine.Networking;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    class ReignOfKingsPlayerManager : IPlayerManager
    {
        private struct PlayerRecord
        {
            public string Nickname;
            public ulong SteamID;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, ReignOfKingsPlayer> players;
        private IDictionary<string, ReignOfKingsLivePlayer> livePlayers;

        internal ReignOfKingsPlayerManager()
        {
            // Load player data
            playerData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, PlayerRecord>>("oxide.covalence.playerdata");
            players = new Dictionary<string, ReignOfKingsPlayer>();
            foreach (var pair in playerData)
            {
                players.Add(pair.Key, new ReignOfKingsPlayer(pair.Value.SteamID, pair.Value.Nickname));
            }
            livePlayers = new Dictionary<string, ReignOfKingsLivePlayer>();
        }

        private void NotifyPlayerJoin(ulong steamid, string nickname)
        {
            string uniqueID = steamid.ToString();

            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(uniqueID, out record))
            {
                // Update
                record.Nickname = nickname;
                playerData[uniqueID] = record;

                // Swap out Rust player
                players.Remove(uniqueID);
                players.Add(uniqueID, new ReignOfKingsPlayer(steamid, nickname));
            }
            else
            {
                // Insert
                record = new PlayerRecord
                {
                    SteamID = steamid,
                    Nickname = nickname
                };
                playerData.Add(uniqueID, record);

                // Create Rust player
                players.Add(uniqueID, new ReignOfKingsPlayer(steamid, nickname));
            }

            // Save
            Interface.Oxide.DataFileSystem.WriteObject("oxide.covalence.playerdata", playerData);
        }

        internal void NotifyPlayerConnect(Player ply)
        {
            NotifyPlayerJoin(ply.Id, ply.Name);
            livePlayers[ply.Id.ToString()] = new ReignOfKingsLivePlayer(ply);
        }

        internal void NotifyPlayerDisconnect(Player ply)
        {
            livePlayers.Remove(ply.Id.ToString());
        }

        #region Offline Players

        /// <summary>
        /// Gets an offline player using their unique ID
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string uniqueID)
        {
            ReignOfKingsPlayer player;
            if (players.TryGetValue(uniqueID, out player))
                return player;
            return null;
        }

        /// <summary>
        /// Gets an offline player using their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer this[int id]
        {
            get
            {
                ReignOfKingsPlayer player;
                return players.TryGetValue(id.ToString(), out player) ? player : null;
            }
        }

        /// <summary>
        /// Gets all offline players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> GetAllPlayers()
        {
            return players.Values.Cast<IPlayer>();
        }

        /// <summary>
        /// Gets all offline players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => players.Values.Cast<IPlayer>();

        /// <summary>
        /// Finds an offline player matching a partial name (case insensitive, null if multiple matches unless exact)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialName)
        {
            return FindPlayers(partialName).SingleOrDefault();
        }

        /// <summary>
        /// Finds any number of offline players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialName)
        {
            return players.Values
                .Where(p => p.Nickname.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                .Cast<IPlayer>();
        }

        #endregion

        #region Online Players

        /// <summary>
        /// Gets an online player given their unique ID
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        public ILivePlayer GetOnlinePlayer(string uniqueID)
        {
            ReignOfKingsLivePlayer player;
            if (livePlayers.TryGetValue(uniqueID, out player))
                return player;
            return null;
        }

        /// <summary>
        /// Gets all online players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ILivePlayer> GetAllOnlinePlayers()
        {
            return livePlayers.Values.Cast<ILivePlayer>();
        }

        /// <summary>
        /// Gets all online players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ILivePlayer> Online => livePlayers.Values.Cast<ILivePlayer>();

        /// <summary>
        /// Finds a single online player matching a partial name (case insensitive, null if multiple matches unless exact)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public ILivePlayer FindOnlinePlayer(string partialName)
        {
            return FindOnlinePlayers(partialName).SingleOrDefault();
        }

        /// <summary>
        /// Finds any number of online players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<ILivePlayer> FindOnlinePlayers(string partialName)
        {
            return livePlayers.Values
                            .Where(p => p.BasePlayer.Nickname.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                            .Cast<ILivePlayer>();
        }

        #endregion
    }
}
