using System;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Steamworks;
using UnityEngine;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class HurtworldPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;
        private readonly PlayerSession session;
        private readonly CSteamID cSteamId;
        private readonly ulong steamId;

        internal HurtworldPlayer(ulong id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            steamId = id;
            Name = name;
            Id = id.ToString();
        }

        internal HurtworldPlayer(PlayerSession session) : this(session.SteamId.m_SteamID, session.Name)
        {
            // Store user object
            this.session = session;
            cSteamId = session.SteamId;
        }

        #region Objects

        /// <summary>
        /// Gets the object that backs the user
        /// </summary>
        public object Object => session;

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
        /// Gets the user's language
        /// </summary>
        public CultureInfo Language => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address => session.Player.ipAddress;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => session.Player.averagePing;

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => session?.IsAdmin ?? GameManager.Instance.IsAdmin(cSteamId);

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned => BanManager.Instance.IsBanned(steamId);

        /// <summary>
        /// Gets if the user is connected
        /// </summary>
        public bool IsConnected => session?.IsLoaded ?? false;

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
        public void Ban(string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (IsBanned) return;

            // Ban and kick user
            BanManager.Instance?.AddBan(steamId);
            if (IsConnected) Kick(reason);
        }

        /// <summary>
        /// Gets the amount of time remaining on the user's ban
        /// </summary>
        public TimeSpan BanTimeRemaining => TimeSpan.MaxValue;

        /// <summary>
        /// Heals the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Heal(float amount)
        {
            var effect = new EntityEffectFluid(EEntityFluidEffectType.Health, EEntityEffectFluidModifierType.AddValuePure, amount);
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            effect.Apply(stats);
        }

        /// <summary>
        /// Gets/sets the user's health
        /// </summary>
        public float Health
        {
            get
            {
                var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
                return stats.GetFluidEffect(EEntityFluidEffectType.Health).GetValue();
            }
            set
            {
                var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
                var effect = stats.GetFluidEffect(EEntityFluidEffectType.Health) as StandardEntityFluidEffect;
                effect?.SetValue(value);
            }
        }

        /// <summary>
        /// Damages the user's character by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            var effect = new EntityEffectFluid(EEntityFluidEffectType.Damage, EEntityEffectFluidModifierType.AddValuePure, -amount);
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            effect.Apply(stats);
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => GameManager.Instance.KickPlayer(session, reason);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            var entityEffectSourceDatum = new EntityEffectSourceData { SourceDescriptionKey = "EntityStats/Sources/Suicide" };
            stats.HandleEvent(new EntityEventData { EventType = EEntityEventType.Die }, entityEffectSourceDatum);
        }

        /// <summary>
        /// Gets/sets the user's maximum health
        /// </summary>
        public float MaxHealth
        {
            get
            {
                var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
                return stats.GetFluidEffect(EEntityFluidEffectType.Health).GetMaxValue();
            }
            set
            {
                var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
                var effect = stats.GetFluidEffect(EEntityFluidEffectType.Health) as StandardEntityFluidEffect;
                effect?.MaxValue(value);
            }
        }

        /// <summary>
        /// Renames the user to specified name
        /// <param name="name"></param>
        /// </summary>
        public void Rename(string name)
        {
            //name = name.Substring(0, 32);
            name = ChatManagerServer.CleanupGeneral(name);
            if (string.IsNullOrEmpty(name.Trim())) name = "Unnamed";

            // Chat/display name
            session.Name = name;
            session.Identity.Name = name;
            session.WorldPlayerEntity.GetComponent<HurtMonoBehavior>().RPC("UpdateName", uLink.RPCMode.All, name);
            SteamGameServer.BUpdateUserData(cSteamId, name, 0);

            // Overhead name
            //var displayProxyName = session.WorldPlayerEntity.GetComponent<DisplayProxyName>();
            //displayProxyName.UpdateName(name);
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => session.WorldPlayerEntity.transform.position = new Vector3(x, y, z);

        /// <summary>
        /// Unbans the user
        /// </summary>
        public void Unban()
        {
            // Check not banned
            if (!IsBanned) return;

            // Set to unbanned
            ConsoleManager.Instance?.ExecuteCommand($"unban {Id}");
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
            var pos = session.WorldPlayerEntity.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the user
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = session.WorldPlayerEntity.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args) => ChatManagerServer.Instance.RPC("RelayChat", session.Player, string.Format(message, args));

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
        public void Command(string command, params object[] args)
        {
            // TODO: Not available yet
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
        public override string ToString() => $"Covalence.HurtworldPlayer[{Id}, {Name}]";

        #endregion
    }
}
