﻿using System;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// A player object that represents the server console
    /// </summary>
    public class RustLegacyConsolePlayer : IPlayer, ILivePlayer
    {
        /// <summary>
        /// Gets the name for this player
        /// </summary>
        public string Name => "Server Console";

        /// <summary>
        /// Gets the ID for this player (unique within the current game)
        /// </summary>
        public string Id => "server_console";

        /// <summary>
        /// Gets the live player if this player is connected
        /// </summary>
        public ILivePlayer ConnectedPlayer => this;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer => this;

        /// <summary>
        /// Gets this player's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character => null;

        #region Permissions

        /// <summary>
        /// Gets if this player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm) => true;

        /// <summary>
        /// Grants the specified permission on this user
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm)
        {
        }

        /// <summary>
        /// Strips the specified permission from this user
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm)
        {
        }

        /// <summary>
        /// Gets if this player belongs to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string groupName) => false;

        /// <summary>
        /// Adds this player to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void AddToGroup(string groupName)
        {
        }

        /// <summary>
        /// Removes this player from the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void RemoveFromGroup(string groupName)
        {
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
        }

        /// <summary>
        /// Unbans this player
        /// </summary>
        public void Unban()
        {
        }

        /// <summary>
        /// Gets if this player is banned
        /// </summary>
        public bool IsBanned => false;

        /// <summary>
        /// Gets the amount of time remaining on this player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.Zero;

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
        }

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill()
        {
        }

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message)
        {
            // TODO: Send message to client
        }

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ConsoleSystem.Run(string.Concat(command, " ", args), true);

        #endregion
    }
}
