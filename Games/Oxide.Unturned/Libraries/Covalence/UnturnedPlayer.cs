using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using SDG.Unturned;
using Steamworks;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Game.Unturned.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class UnturnedPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly SteamPlayer steamPlayer;
        private readonly CSteamID cSteamId;

        internal UnturnedPlayer(ulong id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            Name = name.Sanitize();
            Id = id.ToString();
        }

        internal UnturnedPlayer(SteamPlayer steamPlayer) : this(steamPlayer.playerID.steamID.m_SteamID, steamPlayer.player.name)
        {
            // Store user object
            this.steamPlayer = steamPlayer;
            cSteamId = steamPlayer.playerID.steamID;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the player
        /// </summary>
        public object Object => steamPlayer;

        /// <summary>
        /// Gets the player's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        #endregion Objects

        #region Information

        /// <summary>
        /// Gets/sets the name for the player
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ID for the player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        public string Address
        {
            get
            {
                P2PSessionState_t sessionState;
                SteamGameServerNetworking.GetP2PSessionState(cSteamId, out sessionState);
                return Parser.getIPFromUInt32(sessionState.m_nRemoteIP);
            }
        }

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping => Convert.ToInt32(steamPlayer.ping);

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c => c.EnglishName == steamPlayer.language);

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin => steamPlayer?.isAdmin ?? false;

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned
        {
            get
            {
                SteamBlacklistID steamBlacklistId;
                return SteamBlacklist.checkBanned(cSteamId, Convert.ToUInt32(Address), out steamBlacklistId);
            }
        }

        /// <summary>
        /// Returns if the player is connected
        /// </summary>
        public bool IsConnected => UnityEngine.Time.realtimeSinceStartup - steamPlayer.lastNet < Provider.CLIENT_TIMEOUT;

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping => false;

        /// <summary>
        /// Returns if the player is the server
        /// </summary>
        public bool IsServer => false;

        #endregion Information

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
            Provider.ban(cSteamId, reason, (uint)duration.TotalSeconds);
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        public TimeSpan BanTimeRemaining
        {
            get
            {
                var id = SteamBlacklist.list.First(e => e.playerID.ToString() == Id);
                return TimeSpan.FromSeconds(id.duration);
            }
        }

        /// <summary>
        /// Heals the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount) => steamPlayer.player.life.askHeal((byte)amount, true, true);

        /// <summary>
        /// Gets/sets the player's health
        /// </summary>
        public float Health
        {
            get { return steamPlayer.player.life.health; }
            set { steamPlayer.player.life.tellHealth(cSteamId, (byte)value); }
        }

        /// <summary>
        /// Damages the player's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            EPlayerKill ePlayerKill;
            steamPlayer.player.life.askDamage((byte)amount, Vector3.up * amount, EDeathCause.KILL, ELimb.SKULL, CSteamID.Nil, out ePlayerKill);
        }

        /// <summary>
        /// Kicks the player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => Provider.kick(cSteamId, reason);

        /// <summary>
        /// Causes the player's character to die
        /// </summary>
        public void Kill() => Hurt(101f);

        /// <summary>
        /// Gets/sets the player's maximum health
        /// </summary>
        public float MaxHealth
        {
            get
            {
                return 100f; // TODO: Implement when possible
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
            var angle = steamPlayer.player.transform.rotation.eulerAngles.y;
            steamPlayer.player.sendTeleport(new Vector3(x, y, z), MeasurementTool.angleToByte(angle));
        }

        /// <summary>
        /// Teleports the player's character to the specified generic position
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
            SteamBlacklist.unban(cSteamId);
        }

        #endregion Administration

        #region Location

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            var pos = steamPlayer.player.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the player
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = steamPlayer.player.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion Location

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message and prefix to the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(string message, string prefix, params object[] args)
        {
            message = args.Length > 0 ? string.Format(Formatter.ToUnity(message), args) : Formatter.ToUnity(message);
            var formatted = prefix != null ? $"{prefix} {message}" : message;
            ChatManager.say(cSteamId, formatted, Color.white, EChatMode.LOCAL);
        }

        /// <summary>
        /// Sends the specified message to the player
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => Message(message, null);

        /// <summary>
        /// Replies to the player with the specified message and prefix
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(string message, string prefix, params object[] args) => Message(message, prefix, args);

        /// <summary>
        /// Replies to the player with the specified message
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message, null);

        /// <summary>
        /// Runs the specified console command on the player
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => Commander.execute(cSteamId, $"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");

        #endregion Chat and Commands

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

        #endregion Permissions

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
        public override string ToString() => $"Covalence.UnturnedPlayer[{Id}, {Name}]";

        #endregion Operator Overloads
    }
}
