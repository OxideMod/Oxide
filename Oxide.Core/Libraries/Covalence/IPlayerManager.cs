using System.Collections.Generic;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public interface IPlayerManager
    {
        #region All Players

        /// <summary>
        /// Gets a player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IPlayer GetPlayer(string id);

        /// <summary>
        /// Gets a player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IPlayer this[int id] { get; }

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> All { get; }

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> GetAllPlayers();

        /// <summary>
        /// Finds a single player given a partial name (wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        IPlayer FindPlayer(string partialName);

        /// <summary>
        /// Finds any number of players given a partial name (wildcards accepted)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        IEnumerable<IPlayer> FindPlayers(string partialName);

        #endregion

        #region Connected Players

        /// <summary>
        /// Gets a connected player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IPlayer GetConnectedPlayer(string id);

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> Connected { get; }

        /// <summary>
        /// Finds a single connected player given a partial name (wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        IPlayer FindConnectedPlayer(string partialName);

        /// <summary>
        /// Finds any number of connected players given a partial name (wildcards accepted)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        IEnumerable<IPlayer> FindConnectedPlayers(string partialName);

        #endregion
    }
}
