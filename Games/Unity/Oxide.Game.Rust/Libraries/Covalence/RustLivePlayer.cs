using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class RustLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly ulong steamId;

        /// <summary>
        /// Gets the base player of this player
        /// </summary>
        public IPlayer BasePlayer => RustCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

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

        public ConsoleSystem.Arg LastArg { get; set; }

        /// <summary>
        /// Gets this player's average network ping
        /// </summary>
        public int Ping => Network.Net.sv.GetAveragePing(player.net.connection);

        private readonly BasePlayer player;

        internal RustLivePlayer(BasePlayer player)
        {
            this.player = player;
            steamId = player.userID;
            Character = this;
            Object = player;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Kicks this player from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => player.Kick(reason);

        /// <summary>
        /// Causes this player's character to die
        /// </summary>
        public void Kill() => player.Die();

        /// <summary>
        /// Teleports this player's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            if (player.IsSpectating()) return;

            var dest = new UnityEngine.Vector3(x, y, z);
            player.transform.position = dest;
            player.ClientRPCPlayer(null, player, "ForcePositionTo", dest);
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends a chat message to this player's client
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message) => player.ChatMessage(message);

        /// <summary>
        /// Runs the specified console command on this player's client
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => player.SendConsoleCommand(command, args);

        /// <summary>
        /// Replies to the user
        /// </summary>
        /// <param name="message"></param>
        public void Reply(string message)
        {
            switch (LastCommand)
            {
                case CommandType.Chat:
                    Message(message);
                    return;
                case CommandType.Console:
                    LastArg.ReplyWith(message);
                    break;
            }
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
            var pos = player.transform.position;
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
            var pos = player.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
