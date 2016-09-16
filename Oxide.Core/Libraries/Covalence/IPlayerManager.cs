using System.Collections.Generic;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public interface IPlayerManager
    {
        #region Player Finding

        /// <summary>
        /// Gets a player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IPlayer this[int id] { get; }

        /// <summary>
        /// Gets a player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IPlayer GetPlayer(string id);

        /// <summary>
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> All { get; }
        IEnumerable<IPlayer> GetAllPlayers();

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> Connected { get; }

        /// <summary>
        /// Finds a single player given a partial name or unique ID (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        IPlayer FindPlayer(string partialNameOrId);

        /// <summary>
        /// Finds any number of players given a partial name or unique ID (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        IEnumerable<IPlayer> FindPlayers(string partialNameOrId);

        /// <summary>
        /// Gets a connected player given their unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IPlayer GetConnectedPlayer(string id);

        /// <summary>
        /// Finds a single connected player given a partial name (case-insensitive, wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        IPlayer FindConnectedPlayer(string partialNameOrId);

        /// <summary>
        /// Finds any number of connected players given a partial name (case-insensitive, wildcards accepted)
        /// </summary>
        /// <param name="partialNameOrId"></param>
        /// <returns></returns>
        IEnumerable<IPlayer> FindConnectedPlayers(string partialNameOrId);

        #endregion
    }
}
