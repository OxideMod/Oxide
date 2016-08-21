using System;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class RustLegacyPlayer : IPlayer, IEquatable<IPlayer>, IPlayerCharacter
    {
        private static Permission libPerms;
        private readonly NetUser netUser;
        private readonly ulong steamId;

        internal RustLegacyPlayer(ulong id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            Name = name;
            steamId = id;
            Id = id.ToString();
        }

        internal RustLegacyPlayer(NetUser netUser)
        {
            // Store user details
            this.netUser = netUser;
            steamId = netUser.userID;
            Name = netUser.displayName;
            Id = steamId.ToString();
            Character = this;
            Object = netUser.playerClient.gameObject;
        }

        #region Objects

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of the character
        /// </summary>
        public IPlayer Owner => this;

        /// <summary>
        /// Gets the object that backs this character, if available
        /// </summary>
        public object Object { get; private set; }

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
        public string Address => netUser.networkPlayer.ipAddress;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => netUser.networkPlayer.averagePing;

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => netUser.CanAdmin();

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned => BanList.Contains(steamId);

        /// <summary>
        /// Returns if the user is connected
        /// </summary>
        public bool IsConnected => netUser.connected;

        /// <summary>
        /// Returns if the user is sleeping
        /// </summary>
        public bool IsSleeping => SleepingAvatar.IsOpen(netUser);

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
            BanList.Add(steamId);
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => new DateTime(0, 0, 0) - DateTime.Now; // TODO: Implement somehow?

        /// <summary>
        /// Damages the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            var character = netUser.playerClient.controllable.idMain;
            if (character == null || !character.alive) return;

            TakeDamage.Hurt(character, character, amount);
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            Rust.Notice.Popup(netUser.networkPlayer, "", reason, 10f);
            NetCull.CloseConnection(netUser.networkPlayer, false);
        }

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            var character = netUser.playerClient.controllable.idMain;
            if (character == null || !character.alive) return;

            TakeDamage.Kill(character, character);
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => netUser.playerClient.transform.position = new Vector3(x, y, z);

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
            // Check not banned
            if (!IsBanned) return;

            // Set to unbanned
            BanList.Remove(steamId);
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
            var pos = netUser.playerClient.transform.position;
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
            var pos = netUser.playerClient.transform.position;
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
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"chat.add \"Server\" {string.Format(message, args).Quote()}");
        }

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args)
        {
            switch (LastCommand)
            {
                case CommandType.Chat:
                    Message(message, args);
                    break;
                case CommandType.Console:
                    Command(string.Format($"echo {message}", args));
                    break;
            }
        }

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
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
