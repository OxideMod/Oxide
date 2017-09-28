namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Specifies a provider for core game-specific covalence functionality
    /// </summary>
    public interface ICovalenceProvider
    {
        /// <summary>
        /// Gets the name of the game
        /// </summary>
        string GameName { get; }

        /// <summary>
        /// Gets the Steam app ID of the game's client
        /// </summary>
        uint ClientAppId { get; }

        /// <summary>
        /// Gets the Steam app ID of the game's server
        /// </summary>
        uint ServerAppId { get; }

        /// <summary>
        /// Creates the game-specific command system provider object
        /// </summary>
        /// <returns></returns>
        ICommandSystem CreateCommandSystemProvider();

        /// <summary>
        /// Creates the game-specific player manager object
        /// </summary>
        /// <returns></returns>
        IPlayerManager CreatePlayerManager();

        /// <summary>
        /// Creates the game-specific server object
        /// </summary>
        /// <returns></returns>
        IServer CreateServer();

        /// <summary>
        /// Formats the text with markup into the game-specific markup language
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        string FormatText(string text);
    }
}
