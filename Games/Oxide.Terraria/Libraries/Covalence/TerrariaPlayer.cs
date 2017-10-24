using System;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Terraria;
using Terraria.Localization;

namespace Oxide.Game.Terraria.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class TerrariaPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly Player player;

        internal TerrariaPlayer(string id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            Name = name.Sanitize();
            Id = id;
        }

        internal TerrariaPlayer(Player player) : this(player.whoAmI.ToString(), player.name)
        {
            // Store user object
            this.player = player;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the player
        /// </summary>
        public object Object => player;

        /// <summary>
        /// Gets the player's last command type
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
        /// Gets the player's IP address
        /// </summary>
        public string Address => Netplay.Clients[player.whoAmI].Socket.GetRemoteAddress().ToString();

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping => 0; // TODO: Implement when possible

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin => false; // TODO: Implement when possible

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned => Netplay.IsBanned(Netplay.Clients[player.whoAmI].Socket.GetRemoteAddress());

        /// <summary>
        /// Returns if the player is connected
        /// </summary>
        public bool IsConnected => Netplay.Clients[player.whoAmI].Socket.IsConnected();

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping => false;

        /// <summary>
        /// Returns if the player is the server
        /// </summary>
        public bool IsServer => false;

        #endregion

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (IsBanned) return;

            // Ban and kick user
            Netplay.AddBan(player.whoAmI);
            if (IsConnected) Kick(reason);
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.MaxValue;

        /// <summary>
        /// Heals the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount) => NetMessage.SendData(35, -1, -1, NetworkText.Empty, player.whoAmI, amount);

        /// <summary>
        /// Gets/sets the player's health
        /// </summary>
        public float Health
        {
            get
            {
                return 0f; // TODO: Implement when possible
            }
            set
            {
                // TODO: Implement when possible
            }
        }

        /// <summary>
        /// Damages the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount) => NetMessage.SendData(84, -1, -1, NetworkText.Empty, player.whoAmI, amount);

        /// <summary>
        /// Kicks the player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => NetMessage.SendData(2, player.whoAmI, -1, NetworkText.FromLiteral(reason));

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        public void Kill() => NetMessage.SendData(21, -1, -1, NetworkText.Empty, player.whoAmI);

        /// <summary>
        /// Gets/sets the player's maximum health
        /// </summary>
        public float MaxHealth
        {
            get
            {
                return 0f; // TODO: Implement when possible
            }
            set
            {
                // TODO: Implement when possible
            }
        }

        /// <summary>
        /// Renames the player to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name)
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Teleports the player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            player.Teleport(new Vector2(x, y), 1);
            NetMessage.SendData(65, -1, -1, NetworkText.Empty, 0, player.whoAmI, x, y, 1);
        }

        /// <summary>
        /// eleports the player's character to the specified generic position
        /// </summary>
        /// <param name="pos"></param>
        public void Teleport(GenericPosition pos) => Teleport(pos.X, pos.Y, pos.Z);

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban()
        {
            // Check if unbanned already
            if (!IsBanned) return;

            // Set to unbanned
            if (!File.Exists(Netplay.BanFilePath)) return;
            var name = $"//{Main.player[player.whoAmI].name}";
            var identifier = Netplay.Clients[player.whoAmI].Socket.GetRemoteAddress().GetIdentifier();
            var lines = File.ReadAllLines(Netplay.BanFilePath);
            using (var writer = new StreamWriter(Netplay.BanFilePath))
            {
                foreach (var line in lines)
                    if (!line.Contains(name) && !line.Contains(identifier)) writer.WriteLine(line);
            }
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            x = player.position.X;
            y = player.position.Y;
            z = 0;
        }

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position() => new GenericPosition(player.position.X, player.position.Y, 0);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => NetMessage.SendData(25, player.whoAmI, -1, NetworkText.FromLiteral(message));

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args) => Message(string.Format(message, args));

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args) => Message(message, args);

        /// <summary>
        /// Runs the specified console command on the player
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO: Implement when possible
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
        /// Grants the specified permission on this player
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm) => libPerms.GrantUserPermission(Id, perm, null);

        /// <summary>
        /// Strips the specified permission from this player
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm) => libPerms.RevokeUserPermission(Id, perm);

        /// <summary>
        /// Gets if the player belongs to the specified group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => libPerms.UserHasGroup(Id, group);

        /// <summary>
        /// Adds the player to the specified group
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group) => libPerms.AddUserGroup(Id, group);

        /// <summary>
        /// Removes the player from the specified group
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
        public override string ToString() => $"Covalence.TerrariaPlayer[{Id}, {Name}]";

        #endregion
    }
}
