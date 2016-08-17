﻿using System;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player within a game, either connected or not
    /// </summary>
    public interface IPlayer
    {
        #region Objects

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        IPlayerCharacter Character { get; }

        /// <summary>
        /// Gets the player's last used command type
        /// </summary>
        CommandType LastCommand { get; set; }

        #endregion

        #region Information

        /// <summary>
        /// Gets the name for the player
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        int Ping { get; }

        /// <summary>
        /// Returns if the user is connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns if the user is sleeping
        /// </summary>
        bool IsSleeping { get; }

        #endregion

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        bool IsAdmin { get; }

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        bool IsBanned { get; }

        /// <summary>
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        void Ban(string reason, TimeSpan duration = default(TimeSpan));

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        TimeSpan BanTimeRemaining { get; }

        /// <summary>
        /// Damages user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        void Hurt(float amount);

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        void Kick(string reason);

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        void Kill();

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void Teleport(float x, float y, float z);

        /// <summary>
        /// Unbans the user
        /// </summary>
        void Unban();

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        void Message(string message, params object[] args);

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        void Reply(string message, params object[] args);

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        void Command(string command, params object[] args);

        #endregion

        #region Permissions

        /// <summary>
        /// Gets if the player has the specified permission
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
        /// Gets if the player belongs to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        bool BelongsToGroup(string group);

        /// <summary>
        /// Adds the player to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        void AddToGroup(string group);

        /// <summary>
        /// Removes the player from the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        void RemoveFromGroup(string group);

        #endregion
    }
}
