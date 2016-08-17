﻿using System;
using System.Linq;

using SDG.Unturned;
using Steamworks;
using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Unturned.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class UnturnedPlayer : IPlayer, IEquatable<IPlayer>, IPlayerCharacter
    {
        private static Permission libPerms;
        private readonly SteamPlayer steamPlayer;
        private readonly ulong steamId;

        internal UnturnedPlayer(ulong id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            Name = name;
            steamId = id;
            Id = id.ToString();
        }

        internal UnturnedPlayer(SteamPlayer steamPlayer)
        {
            // Store user details
            this.steamPlayer = steamPlayer;
            steamId = steamPlayer.playerID.steamID.m_SteamID;
            Name = steamPlayer.player.name;
            Id = steamId.ToString();
            Character = this;
            Object = steamPlayer.player.transform.gameObject;
        }


        #region Objects

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; }

        /// <summary>
        /// Gets the owner of the character
        /// </summary>
        public IPlayer Owner => this;

        /// <summary>
        /// Gets the object that backs the character, if available
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion

        #region Information

        /// <summary>
        /// Gets/sets the name for the player
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address
        {
            get
            {
                P2PSessionState_t sessionState;
                SteamGameServerNetworking.GetP2PSessionState(steamPlayer.playerID.steamID, out sessionState);
                return Parser.getIPFromUInt32(sessionState.m_nRemoteIP);
            }
        }

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => Convert.ToInt32(steamPlayer.ping);

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => steamPlayer.isAdmin;

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned
        {
            get
            {
                SteamBlacklistID steamBlacklistId;
                return SteamBlacklist.checkBanned(new CSteamID(steamId), out steamBlacklistId);
            }
        }

        /// <summary>
        /// Returns if the user is connected
        /// </summary>
        public bool IsConnected => UnityEngine.Time.realtimeSinceStartup - steamPlayer.lastNet < Provider.CLIENT_TIMEOUT;

        /// <summary>
        /// Returns if the user is sleeping
        /// </summary>
        public bool IsSleeping => false;

        #endregion

        #region Administration

        /// <summary>
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check already banned
            if (IsBanned) return;

            // Set to banned
            Provider.ban(new CSteamID(steamId), reason, (uint)duration.TotalSeconds);
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining
        {
            get
            {
                var id = SteamBlacklist.list.First(e => e.playerID.ToString() == Id);
                return TimeSpan.FromSeconds(id.duration);
            }
        }

        /// <summary>
        /// Damages user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            EPlayerKill ePlayerKill;
            steamPlayer.player.life.askDamage((byte)amount, Vector3.up * amount, EDeathCause.KILL, ELimb.SKULL, CSteamID.Nil, out ePlayerKill);
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => Provider.kick(steamPlayer.playerID.steamID, reason);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            EPlayerKill ePlayerKill;
            steamPlayer.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.KILL, ELimb.SKULL, CSteamID.Nil, out ePlayerKill);
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            var vector31 = steamPlayer.player.transform.rotation.eulerAngles;
            steamPlayer.player.sendTeleport(new Vector3(x, y, z), MeasurementTool.angleToByte(vector31.y));
        }

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
            // Check not banned
            if (!IsBanned) return;

            // Set to unbanned
            SteamBlacklist.unban(new CSteamID(steamId));
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            var pos = steamPlayer.player.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = steamPlayer.player.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args)
        {
            ChatManager.say(steamPlayer.playerID.steamID, string.Format(message, args), Color.white, EChatMode.LOCAL);
        }

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args) => Message(string.Format(message, args));

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            Commander.execute(steamPlayer.playerID.steamID, $"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
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
        /// Grants the specified permission on this user
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm) => libPerms.GrantUserPermission(Id, perm, null);

        /// <summary>
        /// Strips the specified permission from this user
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm) => libPerms.RevokeUserPermission(Id, perm);

        /// <summary>
        /// Gets if the player belongs to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => libPerms.UserHasGroup(Id, group);

        /// <summary>
        /// Adds the player to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group) => libPerms.AddUserGroup(Id, group);

        /// <summary>
        /// Removes the player from the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        public void RemoveFromGroup(string group) => libPerms.RemoveUserGroup(Id, group);

        #endregion

        #region Operator Overloads

        /// <summary>
        /// Returns if player's ID is equal to another player's ID
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPlayer other) => Id == other.Id;

        /// <summary>
        /// Gets the hash code of the player's ID
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Id.GetHashCode();

        #endregion
    }
}
