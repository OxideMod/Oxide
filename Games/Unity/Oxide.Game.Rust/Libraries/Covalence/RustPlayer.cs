using System;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class RustPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly ulong steamId;

        /// <summary>
        /// Gets/sets the name for this player
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ID for this player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the live player if this player is connected
        /// </summary>
        public ILivePlayer ConnectedPlayer => RustCovalenceProvider.Instance.PlayerManager.GetOnlinePlayer(Id);

        internal RustPlayer(ulong id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            Name = name;
            steamId = id;
            Id = id.ToString();
        }

        #region Permissions

        /// <summary>
        /// Gets if this player has the specified permission
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
        /// Gets if this player belongs to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string groupName) => libPerms.UserHasGroup(Id, groupName);

        /// <summary>
        /// Adds this player to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void AddToGroup(string groupName) => libPerms.AddUserGroup(Id, groupName);

        /// <summary>
        /// Removes this player from the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void RemoveFromGroup(string groupName) => libPerms.RemoveUserGroup(Id, groupName);

        #endregion

        #region Administration

        /// <summary>
        /// Bans this player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration)
        {
            // Check already banned
            if (IsBanned) return; // TODO: Extend ban duration?

            // Set to banned
            ServerUsers.Set(steamId, ServerUsers.UserGroup.Banned, Name, reason);
            ServerUsers.Save();

            // TODO: Set a duration somehow
        }

        /// <summary>
        /// Unbans this player
        /// </summary>
        public void Unban()
        {
            // Check not banned
            if (!IsBanned) return;

            // Set to unbanned
            ServerUsers.Set(steamId, ServerUsers.UserGroup.None, Name, string.Empty);
            ServerUsers.Save();
        }

        /// <summary>
        /// Gets if this player is banned
        /// </summary>
        public bool IsBanned => ServerUsers.Is(steamId, ServerUsers.UserGroup.Banned);

        /// <summary>
        /// Gets the amount of time remaining on this player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => IsBanned ? TimeSpan.MaxValue : TimeSpan.Zero; // TODO: Actually check?

        #endregion

        #region Chat and Commands

        public void Reply(string message) => ConnectedPlayer.Reply(message);

        #endregion

        #region Operator Overloads

        public bool Equals(IPlayer other) => Id == other.Id;

        public override bool Equals(object obj)
        {
            var ply = obj as IPlayer;
            if (ply == null) return false;
            return Id == ply.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"Covalence.RustPlayer[{Id}, {Name}]";

        public static bool operator ==(RustPlayer left, IPlayer right) => left != null && left.Equals(right);

        public static bool operator !=(RustPlayer left, IPlayer right) => left != null && !left.Equals(right);

        #endregion
    }
}
