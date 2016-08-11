using System.Text.RegularExpressions;

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
        /// Gets the base player of the user
        /// </summary>
        public IPlayer BasePlayer => RustCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

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

        public ConsoleSystem.Arg LastArg { get; set; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address => Regex.Replace(player.net.connection.ipaddress, @":{1}[0-9]{1}\d*", "");

        /// <summary>
        /// Gets the user's average network ping
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
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => player.IsAdmin();

        /// <summary>
        /// Damages player by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount) => player.Hurt(amount);

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => player.Kick(reason);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill() => player.Die();

        /// <summary>
        /// Teleports the user's character to the specified position
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
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args) => player.ChatMessage(string.Format(message, args));

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args)
        {
            switch (LastCommand)
            {
                case CommandType.Chat:
                    Message(message, args);
                    return;
                case CommandType.Console:
                    Command("echo", args);
                    break;
            }
        }

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => player.SendConsoleCommand(command, args);

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
            var pos = player.transform.position;
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
            var pos = player.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
