﻿using System;
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

        #region All Players

        /// <summary>
        /// Gets a player using their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string id)
        {
            ReignOfKingsPlayer player;
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
                ReignOfKingsPlayer player;
                return allPlayers.TryGetValue(id.ToString(), out player) ? player : null;
            }
        }

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> GetAllPlayers() => allPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> All => allPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Finds a player matching a partial name (case insensitive, null if multiple matches unless exact)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IPlayer FindPlayer(string partialName) => FindPlayers(partialName).SingleOrDefault();

        /// <summary>
        /// Finds any number of players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindPlayers(string partialName)
        {
            return allPlayers.Values.Where(p => p.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).Cast<IPlayer>();
        }

        #endregion

        #region Connected Players

        /// <summary>
        /// Gets a connected player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer GetConnectedPlayer(string id)
        {
            ReignOfKingsPlayer player;
            return connectedPlayers.TryGetValue(id, out player) ? player : null;
        }

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> GetAllConnectedPlayers() => connectedPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPlayer> Connected => connectedPlayers.Values.Cast<IPlayer>();

        /// <summary>
        /// Finds a single connected player matching a partial name (case insensitive, null if multiple matches unless exact)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IPlayer FindConnectedPlayer(string partialName) => FindConnectedPlayers(partialName).SingleOrDefault();

        /// <summary>
        /// Finds any number of connected players given a partial name (case insensitive)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        public IEnumerable<IPlayer> FindConnectedPlayers(string partialName)
        {
            return connectedPlayers.Values.Where(p => p.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).Cast<IPlayer>();
        }

        #endregion
    }
}
