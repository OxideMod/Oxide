using System;
using System.Net;


namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic server hosting the game instance
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// Gets the public-facing name of the server
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        IPAddress Address { get; }

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        ushort Port { get; }

        #region Console

        /// <summary>
        /// Prints the specified message to the server console
        /// </summary>
        /// <param name="message"></param>
        void Print(string message);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        void RunCommand(string command, params object[] args);

        #endregion
    }
}
