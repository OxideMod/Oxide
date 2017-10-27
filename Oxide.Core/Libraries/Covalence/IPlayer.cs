using System;
using System.Globalization;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player within a game, either connected or not
    /// </summary>
    public interface IPlayer
    {
        #region Objects

        /// <summary>
        /// Gets the object that backs the player
        /// </summary>
        object Object { get; }

        /// <summary>
        /// Gets the player's last used command type
        /// </summary>
        CommandType LastCommand { get; set; }

        #endregion Objects

        #region Information

        /// <summary>
        /// Gets/sets the name for the player
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        int Ping { get; }

        /// <summary>
        /// Gets the player's language
        /// </summary>
        CultureInfo Language { get; }

        /// <summary>
        /// Returns if the player is connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        bool IsSleeping { get; }

        /// <summary>
        /// Returns if the player is the server
        /// </summary>
        bool IsServer { get; }

        #endregion Information

        #region Administration

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        bool IsAdmin { get; }

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        bool IsBanned { get; }

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        void Ban(string reason, TimeSpan duration = default(TimeSpan));

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        TimeSpan BanTimeRemaining { get; }

        /// <summary>
        /// Heals the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        void Heal(float amount);

        /// <summary>
        /// Gets/sets the player's health
        /// </summary>
        float Health { get; set; }

        /// <summary>
        /// Damages the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        void Hurt(float amount);

        /// <summary>
        /// Kicks the player from the game
        /// </summary>
        /// <param name="reason"></param>
        void Kick(string reason);

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        void Kill();

        /// <summary>
        /// Gets/sets the player's maximum health
        /// </summary>
        float MaxHealth { get; set; }

        /// <summary>
        /// Renames the player to specified name
        /// <param name="name"></param>
        /// </summary>
        void Rename(string name);

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void Teleport(float x, float y, float z);

        void Teleport(GenericPosition pos);

        /// <summary>
        /// Unbans the player
        /// </summary>
        void Unban();

        #endregion Administration

        #region Location

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void Position(out float x, out float y, out float z);

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <returns></returns>
        GenericPosition Position();

        #endregion Location

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        void Message(string message);

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        void Message(string message, params object[] args);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        void Reply(string message);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        void Reply(string message, params object[] args);

        /// <summary>
        /// Runs the specified console command on the player
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        void Command(string command, params object[] args);

        #endregion Chat and Commands

        #region Permissions

        /// <summary>
        /// Gets if the player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        bool HasPermission(string perm);

        /// <summary>
        /// Grants the specified permission on this player
        /// </summary>
        /// <param name="perm"></param>
        void GrantPermission(string perm);

        /// <summary>
        /// Strips the specified permission from this player
        /// </summary>
        /// <param name="perm"></param>
        void RevokePermission(string perm);

        /// <summary>
        /// Gets if the player belongs to the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        bool BelongsToGroup(string group);

        /// <summary>
        /// Adds the player to the specified group
        /// </summary>
        /// <param name="group"></param>
        void AddToGroup(string group);

        /// <summary>
        /// Removes the player from the specified group
        /// </summary>
        /// <param name="group"></param>
        void RemoveFromGroup(string group);

        #endregion Permissions
    }

    /// <summary>
    /// Represents a position of a point in 3D space
    /// </summary>
    public class GenericPosition
    {
        public float X, Y, Z;

        public GenericPosition()
        {
        }

        public GenericPosition(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GenericPosition)) return false;
            var pos = (GenericPosition)obj;
            return X.Equals(pos.X) && Y.Equals(pos.Y) && Z.Equals(pos.Z);
        }

        public static bool operator ==(GenericPosition a, GenericPosition b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if ((object)a == null || (object)b == null)
                return false;

            return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z);
        }

        public static bool operator !=(GenericPosition a, GenericPosition b)
        {
            return !(a == b);
        }

        public static GenericPosition operator +(GenericPosition a, GenericPosition b)
        {
            return new GenericPosition(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static GenericPosition operator -(GenericPosition a, GenericPosition b)
        {
            return new GenericPosition(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static GenericPosition operator *(float mult, GenericPosition a)
        {
            return new GenericPosition(a.X * mult, a.Y * mult, a.Z * mult);
        }

        public static GenericPosition operator *(GenericPosition a, float mult)
        {
            return new GenericPosition(a.X * mult, a.Y * mult, a.Z * mult);
        }

        public static GenericPosition operator /(GenericPosition a, float div)
        {
            return new GenericPosition(a.X / div, a.Y / div, a.Z / div);
        }

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() << 2 ^ Z.GetHashCode() >> 2;

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
