namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Specifies a provider for core game-specific covalence functionality
    /// </summary>
    public interface ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game for which this provider provides
        /// </summary>
        string GameName { get; }

        /// <summary>
        /// Gets the Steam app ID of the game's client for which this provider provides
        /// </summary>
        uint ClientAppId { get; }

        /// <summary>
        /// Gets the Steam app ID of the game's server for which this provider provides
        /// </summary>
        uint ServerAppId { get; }

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        IServer CreateServer();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        IPlayerManager CreatePlayerManager();

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        ICommandSystem CreateCommandSystemProvider();

        /// <summary>
        /// Formats the text with markup as specified in Oxide.Core.Libraries.Covalence.Formatter
        /// into the game-specific markup language
        /// </summary>
        /// <param name="text">text to format</param>
        /// <returns>formatted text</returns>
        string FormatText(string text);
    }
}
