using CodeHatch.Common;
using CodeHatch.Engine.Behaviours;
using CodeHatch.Engine.Networking;

using Oxide.Core.Libraries.Covalence;

using UnityEngine;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    class ReignOfKingsLivePlayer : ILivePlayer, IPlayerCharacter
    {
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

        private Player player;
        private readonly ulong steamid;

        internal ReignOfKingsLivePlayer(Player player)
        {
            this.player = player;
            steamid = player.Id;
            Object = player;
        }

        public void Kick(string reason)
        {
            Server.Kick(player, reason);
        }

        public void SendChatMessage(string message)
        {
            player.SendMessage(message);
        }

        public void RunCommand(string command, params object[] args)
        {
            //TODO
            //CommandManager.ExecuteCommand(steamid, command);
        }

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

        public void Kill()
        {
            player.Kill();
        }

        public void Teleport(float x, float y, float z)
        {
            player.CurrentCharacter.Entity.GetOrCreate<CharacterTeleport>().Teleport(new Vector3(x, y, z));
        }
    }
}
