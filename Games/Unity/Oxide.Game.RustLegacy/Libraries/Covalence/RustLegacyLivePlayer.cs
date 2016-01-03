using UnityEngine;

using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class RustLegacyLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly ulong steamid;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer => RustLegacyCovalenceProvider.Instance.PlayerManager.GetPlayer(steamid.ToString());

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

        private readonly NetUser netUser;

        internal RustLegacyLivePlayer(NetUser netUser)
        {
            this.netUser = netUser;
            steamid = netUser.userID;
            Character = this;
            Object = netUser.playerClient;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason)
        {
            ServerManagement.Get().networkView.RPC("KP", netUser.networkPlayer, reason);
            NetCull.CloseConnection(netUser.networkPlayer, true);
        }

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill()
        {
            var character = netUser.playerClient.controllable.idMain;
            if (!character || !character.alive) return;

            DamageEvent damageEvent;
            TakeDamage.Kill(character, character, out damageEvent);
        }

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => netUser.playerClient.transform.position = new Vector3(x, y, z);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"chat.add \"Server\" {message.Quote()}");
        }

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void RunCommand(string command, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, string.Format(command, args));
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void GetPosition(out float x, out float y, out float z)
        {
            var pos = netUser.playerClient.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <returns></returns>
        public GenericPosition GetPosition()
        {
            var pos = netUser.playerClient.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
