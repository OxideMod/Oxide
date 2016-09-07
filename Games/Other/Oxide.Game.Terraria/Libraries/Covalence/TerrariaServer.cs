using System;
using System.Net;

using Terraria;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Terraria.Libraries.Covalence
{
    /// <summary>
    /// Represents the server hosting the game instance
    /// </summary>
    public class TerrariaServer : IServer
    {
        #region Information

        /// <summary>
        /// Gets/sets the public-facing name of the server
        /// </summary>
        public string Name
        {
            get { return Main.worldName; }
            set { Main.worldName = value; }
        }

        /// <summary>
        /// Gets the public-facing IP address of the server, if known
        /// </summary>
        public IPAddress Address => Netplay.ServerIP;

        /// <summary>
        /// Gets the public-facing network port of the server, if known
        /// </summary>
        public ushort Port => (ushort)Netplay.ListenPort;

        /// <summary>
        /// Gets the version or build number of the server
        /// </summary>
        public string Version => Main.versionNumber;

        /// <summary>
        /// Gets the network protocol version of the server
        /// </summary>
        public string Protocol => Main.curRelease.ToString();

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => Main.numPlayers;

        /// <summary>
        /// Gets/sets the maximum players allowed on the server
        /// </summary>
        public int MaxPlayers
        {
            get { return Main.maxNetPlayers; }
            set { Main.maxNetPlayers = value; }
        }

        /// <summary>
        /// Gets/sets the current in-game time on the server
        /// </summary>
        public DateTime Time
        {
            get { return new DateTime((long)Main.time); }
            set { Main.time = value.Second; }
        }

        #endregion

        #region Administration

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            Main.SaveSettings();
            WorldGen.saveAndPlay();
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => NetMessage.SendData(25, -1, -1, message, 255, 255, 0, 160);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO: Implement when possible
        }

        #endregion
    }

    public static class ExtensionMethods
    {
        /// <summary>
        /// Adds compatible style formatting to text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Stylize(this string text)
        {
            // [c/FF0000:text] or [color/FF0000:text]
            return text;
        }
    }
}
