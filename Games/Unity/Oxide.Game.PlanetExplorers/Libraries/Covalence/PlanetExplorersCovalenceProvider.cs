using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.PlanetExplorers.Libraries.Covalence
{
    /// <summary>
    /// Provides Covalence functionality for the game "Planet Explorers"
    /// </summary>
    public class PlanetExplorersCovalenceProvider : ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "PlanetExplorers";

        /// <summary>
        /// Gets the Steam app ID of the game, if available
        /// </summary>
        public uint AppId => 237870;

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static PlanetExplorersCovalenceProvider Instance { get; private set; }

        public PlanetExplorersCovalenceProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public PlanetExplorersPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public PlanetExplorersCommandSystem CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new PlanetExplorersServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager() => PlayerManager = new PlanetExplorersPlayerManager();

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new PlanetExplorersCommandSystem();
    }
}
