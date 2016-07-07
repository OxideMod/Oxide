using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.SevenDays.Libraries.Covalence
{
    /// <summary>
    /// Provides Covalence functionality for the game "7 Days to Die"
    /// </summary>
    public class SevenDaysCovalenceProvider : ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        public string GameName => "7DaysToDie";

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static SevenDaysCovalenceProvider Instance { get; private set; }

        public SevenDaysCovalenceProvider()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public SevenDaysPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public SevenDaysCommandSystem CommandSystem { get; private set; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        public IServer CreateServer() => new SevenDaysServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        public IPlayerManager CreatePlayerManager() => PlayerManager = new SevenDaysPlayerManager();

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        public ICommandSystem CreateCommandSystemProvider() => CommandSystem = new SevenDaysCommandSystem();
    }
}
