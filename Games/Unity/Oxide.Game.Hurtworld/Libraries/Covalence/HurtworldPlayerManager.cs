using System;
using System.Collections.Generic;
using System.Linq;

using ProtoBuf;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class HurtworldPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public ulong Id;
        }

        private IDictionary<string, PlayerRecord> playerData;
        private IDictionary<string, HurtworldPlayer> players;
        private IDictionary<string, HurtworldLivePlayer> livePlayers;

        internal HurtworldPlayerManager()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence") ?? new Dictionary<string, PlayerRecord>();
            players = new Dictionary<string, HurtworldPlayer>();
            foreach (var pair in playerData) players.Add(pair.Key, new HurtworldPlayer(pair.Value.Id, pair.Value.Name));
            livePlayers = new Dictionary<string, HurtworldLivePlayer>();

            // Cleanup old .data
            Cleanup.Add(ProtoStorage.GetFileDataPath("oxide.covalence.playerdata.data"));
        }

        private void NotifyPlayerJoin(ulong steamid, string nickname)
        {
            var id = steamid.ToString();

            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(id, out record))
            {
                // Update
                record.Name = nickname;
                playerData[id] = record;

                // Swap out Rust player
                players.Remove(id);
                players.Add(id, new HurtworldPlayer(steamid, nickname));
            }
            else
            {
                // Insert
                record = new PlayerRecord {Id = steamid, Name = nickname};
                playerData.Add(id, record);

                // Create Rust player
                players.Add(id, new HurtworldPlayer(steamid, nickname));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence");
        }

        internal void NotifyPlayerConnect(PlayerSession session)
        {
            NotifyPlayerJoin((ulong)session.SteamId, session.Name);
            livePlayers[session.SteamId.ToString()] = new HurtworldLivePlayer(session);
        }

        internal void NotifyPlayerDisconnect(PlayerSession session) => livePlayers.Remove(session.SteamId.ToString());

        #region Offline Players

        /// <summary>
        /// Gets an offline player using their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string id)
        {
            HurtworldPlayer player;
            return players.TryGetValue(id, out player) ? player : null;
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
                HurtworldPlayer player;
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
            return players.Values.Where(p => p.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).Cast<IPlayer>();
        }

        #endregion

        #region Online Players

        /// <summary>
        /// Gets an online player given their unique ID
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ILivePlayer GetOnlinePlayer(string Id)
        {
            HurtworldLivePlayer player;
            return livePlayers.TryGetValue(Id, out player) ? player : null;
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
            return livePlayers.Values .Where(p => p.BasePlayer.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).Cast<ILivePlayer>();
        }

        #endregion
    }
}
