using System;
using System.Collections.Generic;
using System.Linq;

using CodeHatch.Engine.Networking;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
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

        public IPlayer GetPlayer(string uniqueID)
        {
            ReignOfKingsPlayer player;
            if (players.TryGetValue(uniqueID, out player))
                return player;
            return null;
        }

        /// <summary>
        /// Gets an offline player given their unique ID
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

        public IEnumerable<IPlayer> GetAllPlayers()
        {
            return players.Values.Cast<IPlayer>();
        }

        /// <summary>
        /// Gets all offline players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => players.Values.Cast<IPlayer>();

        public IPlayer FindPlayer(string partialName)
        {
            return FindPlayers(partialName).SingleOrDefault();
        }

        public IEnumerable<IPlayer> FindPlayers(string partialName)
        {
            return players.Values
                .Where(p => p.Nickname.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                .Cast<IPlayer>();
        }

        public ILivePlayer GetOnlinePlayer(string uniqueID)
        {
            ReignOfKingsLivePlayer player;
            if (livePlayers.TryGetValue(uniqueID, out player))
                return player;
            return null;
        }

        public IEnumerable<ILivePlayer> GetAllOnlinePlayers()
        {
            return livePlayers.Values.Cast<ILivePlayer>();
        }

        /// <summary>
        /// Gets all online players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ILivePlayer> Online => livePlayers.Values.Cast<ILivePlayer>();

        public ILivePlayer FindOnlinePlayer(string partialName)
        {
            return FindOnlinePlayers(partialName).SingleOrDefault();
        }

        public IEnumerable<ILivePlayer> FindOnlinePlayers(string partialName)
        {
            return livePlayers.Values
                            .Where(p => p.BasePlayer.Nickname.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                            .Cast<ILivePlayer>();
        }
    }
}
