using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Terraria.Libraries.Covalence
{
    /// <summary>
    /// Provides Covalence functionality for the game "Terraria"
    /// </summary>
    public class TerrariaCovalenceProvider : ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "Terraria";

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static TerrariaCovalenceProvider Instance { get; private set; }

        public TerrariaCovalenceProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public TerrariaPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public TerrariaCommandSystem CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new TerrariaServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager() => PlayerManager = new TerrariaPlayerManager();

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new TerrariaCommandSystem();
    }
}
