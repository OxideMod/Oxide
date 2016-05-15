using UnityEngine;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class HurtworldLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly ulong steamId;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer => HurtworldCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

        /// <summary>
        /// Gets this player's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of this character
        /// </summary>
        public ILivePlayer Owner => this;

        /// <summary>
        /// Gets the object that backs this character, if available
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// Gets this player's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        /// <summary>
        /// Gets this player's average network ping
        /// </summary>
        public int Ping => session.Player.averagePing;

        private readonly PlayerSession session;

        internal HurtworldLivePlayer(PlayerSession session)
        {
            this.session = session;
            steamId = (ulong)session.SteamId;
            Character = this;
            Object = session.WorldPlayerEntity;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => GameManager.Instance.KickPlayer(steamId.ToString(), reason);

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill()
        {
            var component = session.WorldPlayerEntity.GetComponent<EntityStats>();
            var entityEffectSourceDatum = new EntityEffectSourceData { SourceDescriptionKey = "EntityStats/Sources/Suicide" };
            component.HandleEvent(new EntityEventData { EventType = EEntityEventType.Die }, entityEffectSourceDatum);
        }

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => session.WorldPlayerEntity.transform.position = new Vector3(x, y, z);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => ChatManagerServer.Instance.RPC("RelayChat", session.Player, message);

        /// <summary>
        /// Replies to the user
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message) => Message(message);

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO: Not available yet
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of this character
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
        /// Gets the position of this character
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = session.WorldPlayerEntity.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
