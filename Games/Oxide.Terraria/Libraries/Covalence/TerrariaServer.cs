using Oxide.Core.Libraries.Covalence;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using Terraria;
using Terraria.Localization;

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
        /// Gets the language set by the server
        /// </summary>
        public CultureInfo Language => CultureInfo.InstalledUICulture;

        /// <summary>
        /// Gets the total of players currently on the server
        /// </summary>
        public int Players => Main.ActivePlayersCount;

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

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player for the specified reason and duration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <param name="duration"></param>
        public void Ban(string id, string reason, TimeSpan duration = default(TimeSpan))
        {
            // Check if already banned
            if (IsBanned(id)) return;

            // Ban and kick user
            Netplay.AddBan(int.Parse(id));
            //if (IsConnected) Kick(reason); // TODO: Implement if possible
        }

        /// <summary>
        /// Gets the amount of time remaining on the player's ban
        /// </summary>
        /// <param name="id"></param>
        public TimeSpan BanTimeRemaining(string id) => TimeSpan.MaxValue;

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        /// <param name="id"></param>
        public bool IsBanned(string id) => Netplay.IsBanned(Netplay.Clients[int.Parse(id)].Socket.GetRemoteAddress());

        /// <summary>
        /// Saves the server and any related information
        /// </summary>
        public void Save()
        {
            Main.SaveSettings();
            WorldGen.saveAndPlay();
        }

        /// <summary>
        /// Unbans the player
        /// </summary>
        /// <param name="id"></param>
        public void Unban(string id)
        {
            // Check if unbanned already
            if (!IsBanned(id)) return;

            // Set to unbanned
            if (!File.Exists(Netplay.BanFilePath)) return;
            var whoAmI = int.Parse(id);
            var name = $"//{Main.player[whoAmI].name}";
            var identifier = Netplay.Clients[whoAmI].Socket.GetRemoteAddress().GetIdentifier();
            var lines = File.ReadAllLines(Netplay.BanFilePath);
            using (var writer = new StreamWriter(Netplay.BanFilePath))
            {
                foreach (var line in lines)
                    if (!line.Contains(name) && !line.Contains(identifier)) writer.WriteLine(line);
            }
        }

        #endregion Administration

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all users
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(string message) => NetMessage.SendData(25, -1, -1, NetworkText.FromLiteral(message), 255, 255, 0, 160);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            // TODO: Implement when possible
        }

        #endregion Chat and Commands
    }
}
