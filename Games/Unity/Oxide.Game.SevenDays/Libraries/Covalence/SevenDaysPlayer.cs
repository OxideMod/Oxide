using System;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.SevenDays.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class SevenDaysPlayer : IPlayer, IEquatable<IPlayer>, IPlayerCharacter
    {
        private static Permission libPerms;
        private readonly ClientInfo client;
        private readonly ulong steamId;

        internal SevenDaysPlayer(ulong id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            steamId = id;
            Name = name;
            Id = id.ToString();
        }

        internal SevenDaysPlayer(ClientInfo client)
        {
            this.client = client;
            steamId = Convert.ToUInt64(client.playerId);
            Name = client.playerName;
            Id = client.playerId;
            Character = this;
            Object = GameManager.Instance.World.Players.dict[client.entityId].transform.gameObject;
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
        /// Gets the object that backs the character, if available
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
        public string Address => client.networkPlayer.ipAddress;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => client.ping;

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => GameManager.Instance.adminTools.IsAdmin(client.playerId);

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned => GameManager.Instance.adminTools.IsBanned(Id);

        /// <summary>
        /// Gets if the user is connected
        /// </summary>
        public bool IsConnected => false; // TODO

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
            GameManager.Instance.adminTools.AddBan(Id, null, new DateTime(duration.Ticks), reason);
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => GameManager.Instance.adminTools.GetAdminToolsClientInfo(Id).BannedUntil.TimeOfDay;

        /// <summary>
        /// Damages the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            entity.DamageEntity(new DamageSource(EnumDamageSourceType.Undef), (int)amount, false);
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => GameUtils.KickPlayerForClientInfo(client, reason, GameManager.Instance);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            entity.Kill(DamageResponse.New(new DamageSource(EnumDamageSourceType.Undef), true));
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            entity.transform.position = new Vector3(x, y, z);
        }

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
            // Check not banned
            if (!IsBanned) return;

            // Set to unbanned
            GameManager.Instance.adminTools.RemoveBan(Id);
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
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            var pos = entity.gameObject.transform.position;
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
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            var pos = entity.transform.position;
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
            client.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format(message, args), null, false, null, false));
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
            SdtdConsole.Instance.ExecuteSync($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}", client);
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
