using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Provides Covalence functionality for the game "Reign of Kings"
    /// </summary>
    public class ReignOfKingsCovalenceProvider : ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "ReignOfKings";

        /// <summary>
        /// Gets the Steam app ID of the game, if available
        /// </summary>
        public uint AppId => 381690;

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static ReignOfKingsCovalenceProvider Instance { get; private set; }

        public ReignOfKingsCovalenceProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public ReignOfKingsPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public ReignOfKingsCommandSystem CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new ReignOfKingsServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager() => PlayerManager = new ReignOfKingsPlayerManager();

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new ReignOfKingsCommandSystem();
    }
}
