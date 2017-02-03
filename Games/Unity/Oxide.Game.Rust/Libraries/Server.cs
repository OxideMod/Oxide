using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries
{
    public class Server : Library
    {
        // Covalence references
        internal static readonly RustCovalenceProvider Covalence = RustCovalenceProvider.Instance;
        internal static readonly IServer ServerInstance = Covalence.CreateServer();

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Broadcast(string message, string prefix = null) => ServerInstance.Broadcast(prefix != null ? $"{prefix} {message}" : message);

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Broadcast(string message, string prefix = null, params object[] args) => Broadcast(string.Format(message, args), prefix);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ServerInstance.Command(command, args);

        #endregion
    }
}
