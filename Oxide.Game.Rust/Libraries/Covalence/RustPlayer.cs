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
        private Permission libPerms;

        /// <summary>
        /// Gets the last-known nickname for this player
        /// </summary>
        public string Nickname { get; private set; }

        /// <summary>
        /// Gets a unique ID for this player (unique within the current game)
        /// </summary>
        public string UniqueID { get; private set; }

        /// <summary>
        /// Gets the live player if this player is connected
        /// </summary>
        public ILivePlayer ConnectedPlayer { get { return RustCovalenceProvider.Instance.PlayerManager.GetOnlinePlayer(UniqueID); } }

        private ulong steamid;

        internal RustPlayer(ulong steamID, string nickname)
        {
            // Get perms library
            libPerms = Interface.GetMod().GetLibrary<Permission>();

            // Store user details
            Nickname = nickname;
            steamid = steamID;
            UniqueID = steamID.ToString();
        }

        #region Permissions

        /// <summary>
        /// Gets if this player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm)
        {
            return libPerms.UserHasPermission(UniqueID, perm);
        }

        /// <summary>
        /// Grants the specified permission on this user
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm)
        {
            libPerms.GrantUserPermission(UniqueID, perm, null);
        }

        /// <summary>
        /// Strips the specified permission from this user
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm)
        {
            libPerms.RevokeUserPermission(UniqueID, perm);
        }

        /// <summary>
        /// Gets if this player belongs to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string groupName)
        {
            return libPerms.UserHasGroup(UniqueID, groupName);
        }

        /// <summary>
        /// Adds this player to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void AddToGroup(string groupName)
        {
            libPerms.AddUserGroup(UniqueID, groupName);
        }

        /// <summary>
        /// Removes this player from the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void RemoveFromGroup(string groupName)
        {
            libPerms.RemoveUserGroup(UniqueID, groupName);
        }

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
            ServerUsers.Set(steamid, ServerUsers.UserGroup.Banned, Nickname, reason);
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
            ServerUsers.Set(steamid, ServerUsers.UserGroup.None, Nickname, string.Empty);
            ServerUsers.Save();
        }

        /// <summary>
        /// Gets if this player is banned
        /// </summary>
        public bool IsBanned
        {
            get
            {
                return ServerUsers.Is(steamid, ServerUsers.UserGroup.Banned);
            }
        }

        /// <summary>
        /// Gets the amount of time remaining on this player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining
        {
            get
            {
                if (IsBanned)
                    return TimeSpan.MaxValue;
                else
                    return TimeSpan.Zero;
            }
        }

        #endregion

        #region Operator Overloads

        public bool Equals(IPlayer other)
        {
            return UniqueID == other.UniqueID;
        }

        public override bool Equals(object obj)
        {
            IPlayer ply = obj as IPlayer;
            if (ply == null) return false;
            return UniqueID == ply.UniqueID;
        }

        public override int GetHashCode()
        {
            return UniqueID.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Covalence.RustPlayer[{0},{1}]", UniqueID, Nickname);
        }

        public static bool operator ==(RustPlayer left, IPlayer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RustPlayer left, IPlayer right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}
