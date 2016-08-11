using System.Reflection;

using TNet;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Nomad.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class NomadLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly string Id;

        /// <summary>
        /// Gets the base player of the player
        /// </summary>
        public IPlayer BasePlayer => NomadCovalenceProvider.Instance.PlayerManager.GetPlayer(Id);

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
        public string Address => player.address;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => 0; // TODO

        private readonly TcpPlayer player;

        internal NomadLivePlayer(TcpPlayer player)
        {
            this.player = player;
            Id = player.id.ToString();
            Character = this;
            Object = null; // TODO
        }

        #endregion

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => player.isAdmin;

        /// <summary>
        /// Damages player by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            // TODO
        }

        private readonly MethodInfo removePlayer = typeof(GameServer).GetMethod("RemovePlayer", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => removePlayer.Invoke(player, null); // TODO: Reflection

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            // TODO
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z) => new Vector3(x, y, z);

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args)
        {
            // TODO: Not possible yet?
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
            // TODO: Not possible yet?
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
            x = 0; // TODO
            y = 0;
            z = 0;
        }

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            return new GenericPosition(0, 0, 0); // TODO
        }

        #endregion
    }
}
