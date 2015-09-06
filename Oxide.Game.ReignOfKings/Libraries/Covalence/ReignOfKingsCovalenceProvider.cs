using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Provides Covalence functionality for the game "Reign of Kings"
    /// </summary>
    class ReignOfKingsCovalenceProvider : ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "ReignOfKings";

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static ReignOfKingsCovalenceProvider Instance { get; private set; }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public ReignOfKingsPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public ReignOfKingsCommandSystem CommandSystem { get; private set; }

        public ReignOfKingsCovalenceProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer()
        {
            return new ReignOfKingsServer();
        }

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager()
        {
            return PlayerManager = new ReignOfKingsPlayerManager();
        }

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider()
        {
            return CommandSystem = new ReignOfKingsCommandSystem();
        }
    }
}
