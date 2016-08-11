using System;

using UnityEngine;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.SevenDays.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class SevenDaysLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly ulong steamId;

        /// <summary>
        /// Gets the base player of the player
        /// </summary>
        public IPlayer BasePlayer => SevenDaysCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of the character
        /// </summary>
        public ILivePlayer Owner => this;

        /// <summary>
        /// Gets the object that backs the character, if available
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address => client.networkPlayer.ipAddress;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => client.ping;

        private readonly ClientInfo client;

        internal SevenDaysLivePlayer(ClientInfo client)
        {
            this.client = client;
            steamId = Convert.ToUInt64(client.playerId);
            Character = this;
            Object = GameManager.Instance.World.Players.dict[client.entityId].transform.gameObject;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => GameManager.Instance.adminTools.IsAdmin(client.playerId);

        /// <summary>
        /// Damages player by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            entity.DamageEntity(new DamageSource(EnumDamageSourceType.Undef), (int)amount, false);
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => GameUtils.KickPlayerForClientInfo(client, reason, GameManager.Instance);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            entity.Kill(DamageResponse.New(new DamageSource(EnumDamageSourceType.Undef), true));
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            entity.transform.position = new Vector3(x, y, z);
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
            client.SendPackage(new NetPackageGameMessage(EnumGameMessages.Chat, string.Format(message, args), null, false, null, false));
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
            SdtdConsole.Instance.ExecuteSync($"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}", client);
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            var pos = entity.gameObject.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var entity = GameManager.Instance.World.GetEntity(client.entityId);
            var pos = entity.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
