using System;
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
    }
}
