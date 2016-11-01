using System;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// A player object that represents the server console
    /// </summary>
    public class RustConsolePlayer : IPlayer
    {
        #region Objects

        /// <summary>
        /// Gets the object that backs the user
        /// </summary>
        public object Object => null;

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get { return CommandType.Console; } set {} }

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
        public string Address => "127.0.0.1";

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => 0;

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => true;

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned => false;

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
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration)
        {
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.Zero;

        /// <summary>
        /// Heals the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount)
        {
        }

        /// <summary>
        /// Gets/sets the user's health
        /// </summary>
        public float Health { get; set; }

        /// <summary>
        /// Damages the user's character by specified amount
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
        /// Gets/sets the user's maximum health
        /// </summary>
        public float MaxHealth { get; set; }

        /// <summary>
        /// Renames the user to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name)
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

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of the user
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            x = 0;
            y = 0;
            z = 0;
        }

        /// <summary>
        /// Gets the position of the user
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position() => new GenericPosition(0, 0, 0);

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
        public void Command(string command, params object[] args) => ConsoleSystem.Run.Server.Normal(command, args);

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
