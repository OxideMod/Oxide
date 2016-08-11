using System;

using CodeHatch.Common;
using CodeHatch.Damaging;
using CodeHatch.Engine.Behaviours;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events;
using CodeHatch.Networking.Events.Entities;

using UnityEngine;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class ReignOfKingsLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly ulong steamId;

        /// <summary>
        /// Gets the base player of the user
        /// </summary>
        public IPlayer BasePlayer => ReignOfKingsCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of the character
        /// </summary>
        public ILivePlayer Owner => this;

        /// <summary>
        /// Gets the object that backs this character, if available
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address => player.Connection.IpAddress;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => player.Connection.AveragePing;

        private readonly Player player;

        internal ReignOfKingsLivePlayer(Player player)
        {
            this.player = player;
            steamId = player.Id;
            Character = this;
            Object = player.CurrentCharacter.Prefab;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => player.HasPermission("admin");

        /// <summary>
        /// Damages player by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            var entity = player.CurrentCharacter.Entity;
            var damage = new Damage(player.CurrentCharacter.Prefab, null)
            {
                Amount = amount,
                DamageTypes = DamageType.Unknown,
                Damager = entity
            };
            EventManager.CallEvent(new EntityDamageEvent(player.CurrentCharacter.Entity, damage));
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => Server.Kick(player, reason);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill() => player.Kill();

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            player.CurrentCharacter.Entity.GetOrCreate<CharacterTeleport>().Teleport(new Vector3(x, y, z));
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args) => player.SendMessage(message, args);

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
            CommandManager.ExecuteCommand(steamId, $"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
        }

        #endregion

        #region Location

        public void Position(out float x, out float y, out float z)
        {
            var pos = player.CurrentCharacter.SavedPosition;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        public GenericPosition Position()
        {
            var pos = player.CurrentCharacter.SavedPosition;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
