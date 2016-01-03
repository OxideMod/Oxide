using System.Linq;

using CodeHatch.Common;
using CodeHatch.Engine.Behaviours;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;
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

        private readonly ulong steamid;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer => ReignOfKingsCovalenceProvider.Instance.PlayerManager.GetPlayer(steamid.ToString());

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

        private readonly Player player;

        internal ReignOfKingsLivePlayer(Player player)
        {
            this.player = player;
            steamid = player.Id;
            Character = this;
            Object = player.CurrentCharacter;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => Server.Kick(player, reason);

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill() => player.Kill();

        /// <summary>
        /// Teleports this player's character to the specified position
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
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => player.SendMessage(message);

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void RunCommand(string command, params object[] args)
        {
            CommandManager.ExecuteCommand(steamid, command + " " + string.Join(" ", args.ToList().ConvertAll(a => (string)a).ToArray()));
        }

        #endregion

        #region Location

        public void GetPosition(out float x, out float y, out float z)
        {
            var pos = player.CurrentCharacter.SavedPosition;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        public GenericPosition GetPosition()
        {
            var pos = player.CurrentCharacter.SavedPosition;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
