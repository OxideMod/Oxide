using System;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player within a game, either connected or not
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// Gets the name for this player
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the ID for this player (unique within the current game)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the live player if this player is connected
        /// </summary>
        ILivePlayer ConnectedPlayer { get; }

        #region Permissions

        /// <summary>
        /// Gets if this player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        bool HasPermission(string perm);

        /// <summary>
        /// Grants the specified permission on this user
        /// </summary>
        /// <param name="perm"></param>
        void GrantPermission(string perm);

        /// <summary>
        /// Strips the specified permission from this user
        /// </summary>
        /// <param name="perm"></param>
        void RevokePermission(string perm);

        /// <summary>
        /// Gets if this player belongs to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        bool BelongsToGroup(string group);

        /// <summary>
        /// Adds this player to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        void AddToGroup(string group);

        /// <summary>
        /// Removes this player from the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        void RemoveFromGroup(string group);

        #endregion

        #region Administration

        /// <summary>
        /// Bans this player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        void Ban(string reason, TimeSpan duration);

        /// <summary>
        /// Unbans this player
        /// </summary>
        void Unban();

        /// <summary>
        /// Gets if this player is banned
        /// </summary>
        bool IsBanned { get; }

        /// <summary>
        /// Gets the amount of time remaining on this player's ban
        /// </summary>
        TimeSpan BanTimeRemaining { get; }

        #endregion

        #region Chat and Commands

        void Reply(string message, params object[] args);

        #endregion
    }
}
