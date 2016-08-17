﻿using System;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// A player object that represents the server console
    /// </summary>
    public class RustLegacyConsolePlayer : IPlayer
    {
        #region Objects

        /// <summary>
        /// Gets the live player if the user is connected
        /// </summary>
        public IPlayer ConnectedPlayer => this;

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character => null;

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion

        #region Information

        /// <summary>
        /// Gets the name for the user
        /// </summary>
        public string Name => "Server Console";

        /// <summary>
        /// Gets the ID for the user (unique within the current game)
        /// </summary>
        public string Id => "server_console";

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address => Rust.Steam.Server.SteamServer_GetPublicIP().ToString();

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => 0;

        /// <summary>
        /// Returns if the user is connected
        /// </summary>
        public bool IsConnected => true;

        /// <summary>
        /// Returns if the user is sleeping
        /// </summary>
        public bool IsSleeping => false;

        #endregion

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => true;

        /// <summary>
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration)
        {
        }

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
        }

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned => false;

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.Zero;

        /// <summary>
        /// Damages user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
        }

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
        }

        /// <summary>
        /// Teleports the user's character to the specified position
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
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args) => Interface.Oxide.LogInfo(string.Format(message, args));

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
        public void Command(string command, params object[] args) => ConsoleSystem.Run($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");

        #endregion

        #region Permissions

        /// <summary>
        /// Gets if the user has the specified permission
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
        /// Gets if the user belongs to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => false;

        /// <summary>
        /// Adds the user to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group)
        {
        }

        /// <summary>
        /// Removes the user from the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        public void RemoveFromGroup(string group)
        {
        }

        #endregion
    }
}
