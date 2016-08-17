﻿using System;
using System.Collections.Generic;
using System.Linq;

using ProtoBuf;
using Steamworks;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.TheForest.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public class TheForestPlayerManager : IPlayerManager
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private struct PlayerRecord
        {
            public string Name;
            public ulong Id;
        }

        private readonly IDictionary<string, PlayerRecord> playerData;
        private readonly IDictionary<string, TheForestPlayer> allPlayers;
        private readonly IDictionary<string, TheForestPlayer> connectedPlayers;

        internal TheForestPlayerManager()
        {
            // Load player data
            Utility.DatafileToProto<Dictionary<string, PlayerRecord>>("oxide.covalence");
            playerData = ProtoStorage.Load<Dictionary<string, PlayerRecord>>("oxide.covalence") ?? new Dictionary<string, PlayerRecord>();
            allPlayers = new Dictionary<string, TheForestPlayer>();
            foreach (var pair in playerData) allPlayers.Add(pair.Key, new TheForestPlayer(pair.Value.Id, pair.Value.Name));
            connectedPlayers = new Dictionary<string, TheForestPlayer>();
        }

        private void NotifyPlayerJoin(BoltEntity entity)
        {
            var steamId = entity.source.RemoteEndPoint.SteamId.Id;
            var id = entity.source.RemoteEndPoint.SteamId.Id.ToString();
            var name = SteamFriends.GetFriendPersonaName(new CSteamID(entity.source.RemoteEndPoint.SteamId.Id));

            // Do they exist?
            PlayerRecord record;
            if (playerData.TryGetValue(id, out record))
            {
                // Update
                record.Name = SteamFriends.GetFriendPersonaName(new CSteamID(entity.source.RemoteEndPoint.SteamId.Id));
                playerData[id] = record;

                // Swap out Rust player
                allPlayers.Remove(id);
                allPlayers.Add(id, new TheForestPlayer(entity));
            }
            else
            {
                // Insert
                record = new PlayerRecord { Id = steamId, Name = name };
                playerData.Add(id, record);

                // Create Rust player
                allPlayers.Add(id, new TheForestPlayer(steamId, name));
            }

            // Save
            ProtoStorage.Save(playerData, "oxide.covalence");
        }

        internal void NotifyPlayerConnect(BoltEntity entity)
        {
            var id = entity.source.RemoteEndPoint.SteamId.Id;
            connectedPlayers[id.ToString()] = new TheForestPlayer(entity);
        }

        internal void NotifyPlayerDisconnect(BoltEntity entity) => connectedPlayers.Remove(entity.source.RemoteEndPoint.SteamId.Id.ToString());

        #region All Players

        /// <summary>
        /// Gets a player using their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IPlayer GetPlayer(string id)
        {
            TheForestPlayer player;
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
                TheForestPlayer player;
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
            TheForestPlayer player;
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
            return connectedPlayers.Values .Where(p => p.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).Cast<IPlayer>();
        }

        #endregion
    }
}
