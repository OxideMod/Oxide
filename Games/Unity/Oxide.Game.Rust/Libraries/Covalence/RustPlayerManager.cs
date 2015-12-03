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
            public string Nickname;
            public ulong SteamID;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, RustPlayer> players;
        private IDictionary<string, RustLivePlayer> livePlayers;

        internal void Initialize()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence.playerdata");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence.playerdata") ?? new Dictionary<string, PlayerRecord>();
            players = new Dictionary<string, RustPlayer>();
            foreach (var pair in playerData) players.Add(pair.Key, new RustPlayer(pair.Value.SteamID, pair.Value.Nickname));
            livePlayers = new Dictionary<string, RustLivePlayer>();
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
                players.Add(uniqueId, new RustPlayer(steamid, nickname));
            }
            else
            {
                // Insert
                record = new PlayerRecord {SteamID = steamid, Nickname = nickname};
                playerData.Add(uniqueId, record);

                // Create Rust player
                players.Add(uniqueId, new RustPlayer(steamid, nickname));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence.playerdata");
        }

        internal void NotifyPlayerConnect(BasePlayer ply)
        {
            NotifyPlayerJoin(ply.userID, ply.net.connection.username);
            livePlayers[ply.userID.ToString()] = new RustLivePlayer(ply);
        }

        internal void NotifyPlayerDisconnect(BasePlayer ply) => livePlayers.Remove(ply.userID.ToString());

        #region Offline Players

        /// <summary>
        /// Gets an offline player using their unique ID
        /// </summary>
        /// <param name="uniqueId"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string uniqueId)
        {
            RustPlayer player;
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
                RustPlayer player;
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
