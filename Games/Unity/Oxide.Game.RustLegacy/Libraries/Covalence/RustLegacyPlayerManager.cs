using System;
using System.Collections.Generic;
using System.Linq;

using ProtoBuf;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class RustLegacyPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Nickname;
            public ulong SteamId;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, RustLegacyPlayer> players;
        private IDictionary<string, RustLegacyLivePlayer> livePlayers;

        internal RustLegacyPlayerManager()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence.playerdata");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence.playerdata") ?? new Dictionary<string, PlayerRecord>();
            players = new Dictionary<string, RustLegacyPlayer>();
            foreach (var pair in playerData) players.Add(pair.Key, new RustLegacyPlayer(pair.Value.SteamId, pair.Value.Nickname));
            livePlayers = new Dictionary<string, RustLegacyLivePlayer>();
        }

        private void NotifyPlayerJoin(ulong steamid, string nickname)
        {
            var uniqueId = steamid.ToString();

            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(uniqueId, out record))
            {
                // Update
                record.Nickname = nickname;
                playerData[uniqueId] = record;

                // Swap out Rust player
                players.Remove(uniqueId);
                players.Add(uniqueId, new RustLegacyPlayer(steamid, nickname));
            }
            else
            {
                // Insert
                record = new PlayerRecord {SteamId = steamid, Nickname = nickname};
                playerData.Add(uniqueId, record);

                // Create Rust player
                players.Add(uniqueId, new RustLegacyPlayer(steamid, nickname));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence.playerdata");
        }

        internal void NotifyPlayerConnect(NetUser netUser)
        {
            NotifyPlayerJoin(netUser.userID, netUser.displayName);
            livePlayers[netUser.userID.ToString()] = new RustLegacyLivePlayer(netUser);
        }

        internal void NotifyPlayerDisconnect(NetUser netUser) => livePlayers.Remove(netUser.userID.ToString());

        #region Offline Players

        /// <summary>
        /// Gets an offline player using their unique ID
        /// </summary>
        /// <param name="uniqueId"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string uniqueId)
        {
            RustLegacyPlayer player;
            return players.TryGetValue(uniqueId, out player) ? player : null;
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
                RustLegacyPlayer player;
                return players.TryGetValue(id.ToString(), out player) ? player : null;
            }
        }

        /// <summary>
        /// Gets all offline players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> GetAllPlayers() => players.Values.Cast<IPlayer>();

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
        public IPlayer FindPlayer(string partialName) => FindPlayers(partialName).SingleOrDefault();

        /// <summary>
        /// Finds any number of offline players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialName)
        {
            return players.Values.Where(p => p.Nickname.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).Cast<IPlayer>();
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
            RustLegacyLivePlayer player;
            return livePlayers.TryGetValue(uniqueID, out player) ? player : null;
        }

        /// <summary>
        /// Gets all online players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ILivePlayer> GetAllOnlinePlayers() => livePlayers.Values.Cast<ILivePlayer>();

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
        public ILivePlayer FindOnlinePlayer(string partialName) => FindOnlinePlayers(partialName).SingleOrDefault();

        /// <summary>
        /// Finds any number of online players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<ILivePlayer> FindOnlinePlayers(string partialName)
        {
            return livePlayers.Values .Where(p => p.BasePlayer.Nickname.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).Cast<ILivePlayer>();
        }

        #endregion
    }
}
