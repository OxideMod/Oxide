using System;
using System.Globalization;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Steamworks;
using TheForest.Utils;
using UdpKit;
using UnityEngine;

namespace Oxide.Game.TheForest.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class TheForestPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly BoltEntity entity;
        private readonly CSteamID cSteamId;
        private readonly ulong steamId;

        internal TheForestPlayer(ulong id, string name)
        {
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            Name = name.Sanitize();
            steamId = id;
            Id = id.ToString();
        }

        internal TheForestPlayer(BoltEntity entity)
        {
            steamId = entity.source.RemoteEndPoint.SteamId.Id;
            cSteamId = new CSteamID(steamId);
            Id = steamId.ToString();
            Name = entity.GetState<IPlayerState>().name.Sanitize();
            this.entity = entity;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the player
        /// </summary>
        public object Object => entity;

        /// <summary>
        /// Gets the player's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion

        #region Information

        /// <summary>
        /// Gets the name for the player
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        public string Address
        {
            get
            {
                P2PSessionState_t sessionState;
                SteamGameServerNetworking.GetP2PSessionState(cSteamId, out sessionState);
                var ip = sessionState.m_nRemoteIP;
                return string.Concat(ip >> 24 & 255, ".", ip >> 16 & 255, ".", ip >> 8 & 255, ".", ip & 255);
            }
        }

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping => Convert.ToInt32(entity.source.PingNetwork);

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin => entity.source.IsDedicatedServerAdmin();

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned => CoopKick.IsBanned(new UdpSteamID(steamId));

        /// <summary>
        /// Returns if the player is connected
        /// </summary>
        public bool IsConnected => BoltNetwork.clients.Contains(entity.source); // TODO: Test

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping => Scene.Atmosphere.Sleeping; // TODO: Fix

        /// <summary>
        /// Returns if the player is the server
        /// </summary>
        public bool IsServer => false;

        #endregion

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            if (IsBanned) return;

            var kickedPlayer = new CoopKick.KickedPlayer()
            {
                Name = Name,
                SteamId = steamId,
                BanEndTime = (duration.TotalMinutes <= 0 ? 0 : DateTime.UtcNow.ToUnixTimestamp() + (long)duration.TotalMinutes)
            };
            CoopKick.Instance.kickedSteamIds.Add(kickedPlayer);
            CoopKick.SaveList();
            if (IsConnected) Kick(reason);
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining
        {
            get
            {
                var kickedPlayer = CoopKick.Instance.KickedPlayers.First(k => k.SteamId == steamId);
                return kickedPlayer != null ? TimeSpan.FromTicks(kickedPlayer.BanEndTime) : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Heals the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount) => entity.GetComponentInChildren<PlayerStats>().Health += amount;

        /// <summary>
        /// Gets/sets the player's health
        /// </summary>
        public float Health
        {
            get { return entity.GetComponentInChildren<PlayerStats>().Health; }
            set { entity.GetComponentInChildren<PlayerStats>().Health = value; }
        }

        /// <summary>
        /// Damages the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount) => entity.GetComponentInChildren<PlayerStats>().Hit((int)amount, true);

        /// <summary>
        /// Kicks the player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            var connection = entity.source;
            var coopKickToken = new CoopKickToken()
            {
                KickMessage = reason,
                Banned = false
            };
            connection.Disconnect(coopKickToken);
        }

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        public void Kill() => Hurt(1000f);

        /// <summary>
        /// Gets/sets the player's maximum health
        /// </summary>
        public float MaxHealth
        {
            get
            {
                return 1000f; // TODO: Implement when possible
            }
            set
            {
                // TODO: Implement when possible
            }
        }

        /// <summary>
        /// Renames the player to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name) => entity.GetState<IPlayerState>().name = name; // TODO: Test

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => entity.gameObject.transform.position = new Vector3(x, y, z);

        /// <summary>
        /// Teleports the player's character to the specified generic position
        /// </summary>
        /// <param name="pos"></param>
        public void Teleport(GenericPosition pos) => Teleport(pos.X, pos.Y, pos.Z);

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban()
        {
            if (!IsBanned) return;

            var kickedPlayer = CoopKick.Instance.kickedSteamIds.First(k => k.SteamId == steamId);
            if (kickedPlayer != null)
            {
                CoopKick.Instance.kickedSteamIds.Remove(kickedPlayer);
                CoopKick.SaveList();
            }
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            var pos = entity.gameObject.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = entity.gameObject.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message)
        {
            CoopServerInfo.Instance.entity.GetState<IPlayerState>().name = "Server";

            var chatEvent = ChatEvent.Create(entity.source);
            chatEvent.Message = message;
            chatEvent.Sender = CoopServerInfo.Instance.entity.networkId;
            chatEvent.Send();
            //CoopAdminCommand.SendNetworkMessage
        }

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args) => Message(string.Format(message, args));

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args) => Message(message, args);

        /// <summary>
        /// Runs the specified console command on the player
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            var adminCommand = AdminCommand.Create(entity.source);
            adminCommand.Command = command;
            adminCommand.Data = string.Concat(args.Select(o => o.ToString()).ToArray());
            adminCommand.Send();
        }

        #endregion

        #region Permissions

        /// <summary>
        /// Gets if the player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm) => libPerms.UserHasPermission(Id, perm);

        /// <summary>
        /// Grants the specified permission on this player
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm) => libPerms.GrantUserPermission(Id, perm, null);

        /// <summary>
        /// Strips the specified permission from this player
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm) => libPerms.RevokeUserPermission(Id, perm);

        /// <summary>
        /// Gets if the player belongs to the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => libPerms.UserHasGroup(Id, group);

        /// <summary>
        /// Adds the player to the specified group
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group) => libPerms.AddUserGroup(Id, group);

        /// <summary>
        /// Removes the player from the specified group
        /// </summary>
        /// <param name="group"></param>
        public void RemoveFromGroup(string group) => libPerms.RemoveUserGroup(Id, group);

        #endregion

        #region Operator Overloads

        /// <summary>
        /// Returns if player's unique ID is equal to another player's unique ID
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPlayer other) => Id == other?.Id;

        /// <summary>
        /// Returns if player's object is equal to another player's object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is IPlayer && Id == ((IPlayer)obj).Id;

        /// <summary>
        /// Gets the hash code of the player's unique ID
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Returns a human readable string representation of this IPlayer
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Covalence.TheForestPlayer[{Id}, {Name}]";

        #endregion
    }
}
