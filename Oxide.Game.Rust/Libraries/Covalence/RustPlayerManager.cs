using System;
using System.Collections.Generic;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class RustPlayerManager : IPlayerManager
    {
        private struct PlayerRecord
        {
            public string Nickname;
            public ulong SteamID;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, RustPlayer> players;
        private IDictionary<string, RustLivePlayer> livePlayers;

        internal void Initialize()
        {
            // Load player data
            playerData = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, PlayerRecord>>("oxide.covalence.playerdata");
            players = new Dictionary<string, RustPlayer>();
            foreach (var pair in playerData)
            {
                players.Add(pair.Key, new RustPlayer(pair.Value.SteamID, pair.Value.Nickname));
            }
            livePlayers = new Dictionary<string, RustLivePlayer>();
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
                players.Add(uniqueID, new RustPlayer(steamid, nickname));
            }
            else
            {
                // Insert
                record = new PlayerRecord();
                record.SteamID = steamid;
                record.Nickname = nickname;
                playerData.Add(uniqueID, record);

                // Create Rust player
                players.Add(uniqueID, new RustPlayer(steamid, nickname));
            }

            // Save
            Interface.GetMod().DataFileSystem.WriteObject("oxide.covalence.playerdata", playerData);
        }

        internal void NotifyPlayerConnect(BasePlayer ply)
        {
            NotifyPlayerJoin(ply.userID, ply.net.connection.username);
            livePlayers[ply.userID.ToString()] = new RustLivePlayer(ply);
        }

        internal void NotifyPlayerDisconnect(BasePlayer ply)
        {
            livePlayers.Remove(ply.userID.ToString());
        }

        #region Offline Players

        /// <summary>
        /// Gets an offline player given their unique ID
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string uniqueID)
        {
            RustPlayer player;
            if (players.TryGetValue(uniqueID, out player))
                return player;
            else
                return null;
        }

        /// <summary>
        /// Gets an offline player given their unique ID
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        public IPlayer this[int id]
        {
            get
            {
                RustPlayer player;
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
            var name = partialName.ToLower();
            var players = FindPlayers(partialName);
            return players.SingleOrDefault() ?? players.FirstOrDefault(pl => pl.Nickname.ToLower() == name);
        }

        /// <summary>
        /// Finds any number of offline players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialName)
        {
            var name = partialName.ToLower();
            return players.Values.Where(p => p.Nickname.ToLower().Contains(name)).Cast<IPlayer>();
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
            RustLivePlayer player;
            if (livePlayers.TryGetValue(uniqueID, out player))
                return player;
            else
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
            var name = partialName.ToLower();
            var players = FindOnlinePlayers(partialName);
            return players.SingleOrDefault() ?? players.FirstOrDefault(pl => pl.BasePlayer.Nickname.ToLower() == name);
        }

        /// <summary>
        /// Finds any number of online players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<ILivePlayer> FindOnlinePlayers(string partialName)
        {
            var name = partialName.ToLower();
            return livePlayers.Values.Where(p => p.BasePlayer.Nickname.ToLower().Contains(name)).Cast<ILivePlayer>();
        }

        #endregion
    }
}
