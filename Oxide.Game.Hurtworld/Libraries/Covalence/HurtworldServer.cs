using System.Linq;
using System.Net;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    class HurtworldServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => GameManager.Instance.ServerConfig.GameName;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => IPAddress.Parse(uLink.MasterServer.ipAddress);

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)uLink.MasterServer.port;

        #endregion

        #region Console and Commands

        /// <summary>
        /// Prints the specified message to the server console
        /// </summary>
        /// <param name="message"></param>
        public void Print(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void RunCommand(string command, params object[] args)
        {
            // TODO
        }

        #endregion
    }
}
