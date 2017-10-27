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
        /// Gets all players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> All { get; }

        /// <summary>
        /// Gets all connected players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> Connected { get; }

        /// <summary>
        /// Gets all sleeping players
        /// </summary>
        /// <returns></returns>
        //IEnumerable<IPlayer> Sleeping { get; }

        /// <summary>
        /// Finds a single player given unique ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IPlayer FindPlayerById(string id);

        /// <summary>
        /// Finds a single connected player given game object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        IPlayer FindPlayerByObj(object obj);

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

        #endregion Player Finding
    }
}
