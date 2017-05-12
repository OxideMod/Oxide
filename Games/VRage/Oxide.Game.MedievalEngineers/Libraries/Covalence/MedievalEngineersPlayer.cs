using System;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace Oxide.Game.MedievalEngineers.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class MedievalEngineersPlayer : IPlayer, IEquatable<IPlayer>
    {
        #region Initialization

        internal readonly Player Player = new Player();

        private static Permission libPerms;
        private readonly IMyPlayer player;
        private readonly MyPlayer myPlayer;
        private readonly ulong steamId;

        internal MedievalEngineersPlayer(ulong id, string name)
        {
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            steamId = id;
            Name = name;
            Id = id.ToString();
        }

        internal MedievalEngineersPlayer(IMyPlayer player) : this(player.SteamUserId, player.DisplayName)
        {
            this.player = player;
            this.myPlayer = player as MyPlayer;
        }

        #endregion

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
        public string Address => Player.Address(player);

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => Player.Ping(player);

        /// <summary>
        /// Gets the user's language
        /// </summary>
        public CultureInfo Language => Player.Language(player);

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => Player.IsAdmin(player); // TODO: Switch to steamId

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned => Player.IsBanned(steamId);

        /// <summary>
        /// Gets if the user is connected
        /// </summary>
        public bool IsConnected => Player.IsConnected(player);

        /// <summary>
        /// Returns if the user is sleeping
        /// </summary>
        public bool IsSleeping => Player.IsSleeping(steamId);

        #endregion

        #region Administration
 /// <summary>
        /// Bans the user for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            if (!IsBanned) Player.Ban(player, reason);
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.MaxValue;

        /// <summary>
        /// Heals the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount) => Player.Heal(player, amount);

        /// <summary>
        /// Gets/sets the user's health
        /// </summary>
        public float Health
        {
            get { return myPlayer.Character.StatComp.Health.Value; }
            set { myPlayer.Character.StatComp.Health.Value = value; }
        }

        /// <summary>
        /// Damages the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount) => Player.Hurt(player, amount);

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => Player.Kick(player, reason);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill() => Player.Kill(player);

        /// <summary>
        /// Gets/sets the user's maximum health
        /// </summary>
        public float MaxHealth
        {
            get { return myPlayer.Character.StatComp.Health.MaxValue; }
            set { myPlayer.Character.StatComp.Health.m_maxValue = value; }
        }

        /// <summary>
        /// Renames the user to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name) => Player.Rename(player, name);

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => Player.Teleport(player, x, y, z);

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
            if (IsBanned) return;  Player.Unban(player);
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
            var pos = Player.Position(player);
            x = (float)pos.X;
            y = (float)pos.Y;
            z = (float)pos.Z;
        }

        /// <summary>
        /// Gets the position of the user
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = Player.Position(player);
            return new GenericPosition((float)pos.X, (float)pos.Y, (float)pos.Z);
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => Player.Message(player, message);

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args) => Message(string.Format(message, args));

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message);

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args) => Message(message, args);

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => Player.Command(player, command, args);

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
        /// Returns if player's unique ID is equal to another player's unique ID
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPlayer other) => Id == other?.Id;

        /// <summary>
        /// Returns if player's object is equal to another player's object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is IPlayer && Id == ((IPlayer)obj).Id;

        /// <summary>
        /// Gets the hash code of the player's unique ID
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Returns a human readable string representation of this IPlayer
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Covalence.MedievalEngineersPlayer[{Id}, {Name}]";

        #endregion
    }
}
