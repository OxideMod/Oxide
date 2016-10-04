using System;
using System.Reflection;

using TNet;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Nomad.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class NomadPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly TcpPlayer player;

        internal NomadPlayer(string id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            Name = name;
            Id = id;
        }

        internal NomadPlayer(TcpPlayer player) : this(player.id.ToString(), player.name)
        {
            // Store user object
            this.player = player;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the user
        /// </summary>
        public object Object => player;

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion

        #region Information

        /// <summary>
        /// Gets the name for the player
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address => player.address;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => 0; // TODO: Implement when possible

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => player.isAdmin;

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned => false; // TODO: Implement when possible

        /// <summary>
        /// Gets if the user is connected
        /// </summary>
        public bool IsConnected => player.isConnected;

        /// <summary>
        /// Returns if the user is sleeping
        /// </summary>
        public bool IsSleeping => false; // TODO: Implement when possible

        #endregion

        #region Administration

        private readonly MemberInfo[] banList = typeof(GameServer).GetMember("mBan", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo saveList = typeof(Tools).GetMethod("SaveList", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (IsBanned) return;

            // Ban and kick user
            // TODO: Implement when possible
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.Zero; // TODO: Implement when possible

        /// <summary>
        /// Heals the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount)
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Gets/sets the user's health
        /// </summary>
        public float Health
        {
            get { return 0f; } // TODO: Implement when possible
            set { } // TODO: Implement when possible
        }

        /// <summary>
        /// Damages the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            // TODO: Implement when possible
        }

        private readonly MethodInfo removePlayer = typeof(GameServer).GetMethod("RemovePlayer", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => removePlayer?.Invoke(player, null); // TODO: Reflection

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Gets/sets the user's maximum health
        /// </summary>
        public float MaxHealth
        {
            get
            {
                return 0; // TODO: Implement when possible
            }
            set
            {
                // TODO: Implement when possible
            }
        }

        /// <summary>
        /// Renames the user to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name)
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => new Vector3(x, y, z); // TODO: Implement when possible

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
            // Check not banned
            if (!IsBanned) return;

            // Set to unbanned
            // TODO: Implement when possible
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
            x = 0; // TODO: Implement when possible
            y = 0;
            z = 0;
        }

        /// <summary>
        /// Gets the position of the user
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            return new GenericPosition(0, 0, 0); // TODO: Implement when possible
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
            // TODO: Not possible yet?
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
            // TODO: Not possible yet?
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
