using System.Linq;
using System.Net;

using CodeHatch.Build;
using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    class ReignOfKingsServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        public string Name => DedicatedServerBypass.Settings?.ServerName;

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => IPAddress.Parse(CodeHatch.Engine.Core.Gaming.Game.ServerData.IP);

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => CodeHatch.Engine.Core.Gaming.Game.ServerData.Port;

        /// <summary>
        /// Gets the version number/build of the server
        /// </summary>
        public string Version => GameInfo.VersionName.ToLower();

        #endregion

        #region Commands

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void RunCommand(string command, params object[] args)
        {
            CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, command + " " + string.Join(" ", args.ToList().ConvertAll(a => (string)a).ToArray()));
        }

        #endregion

        #region Console/Logging

        /// <summary>
        /// Prints an info message to the server console/log
        /// </summary>
        /// <param name="message"></param>
        public void Print(string message) => UnityEngine.Debug.Log(message);

        /// <summary>
        /// Prints a warning message to the server console/log
        /// </summary>
        /// <param name="message"></param>
        public void PrintWarning(string message) => UnityEngine.Debug.LogWarning(message);

        /// <summary>
        /// Prints an error message to the server console/log
        /// </summary>
        /// <param name="message"></param>
        public void PrintError(string message) => UnityEngine.Debug.LogError(message);

        #endregion
    }
}
