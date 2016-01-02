using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.RustLegacy.Libraries.Covalence
{
    /// <summary>
    /// Provides Covalence functionality for the game "Rust Legacy"
    /// </summary>
    public class RustLegacyCovalenceProvider : ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "Rust Legacy";

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static RustLegacyCovalenceProvider Instance { get; private set; }

        public RustLegacyCovalenceProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public RustLegacyPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public RustLegacyCommandSystem CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new RustLegacyServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager() => PlayerManager = new RustLegacyPlayerManager();

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new RustLegacyCommandSystem();
    }
}
