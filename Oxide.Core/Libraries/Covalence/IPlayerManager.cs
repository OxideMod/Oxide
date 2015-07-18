using System;
using System.Collections.Generic;


namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a generic player manager
    /// </summary>
    public interface IPlayerManager
    {
        #region Offline Players

        /// <summary>
        /// Gets an offline player given their unique ID
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        IPlayer GetPlayer(string uniqueID);

        /// <summary>
        /// Gets an offline player given their unique ID
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        IPlayer this[int id] { get; }

        /// <summary>
        /// Gets all offline players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> GetAllPlayers();

        /// <summary>
        /// Gets all offline players
        /// </summary>
        /// <returns></returns>
        IEnumerable<IPlayer> All { get; }

        /// <summary>
        /// Finds a single offline player given a partial name (wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        IPlayer FindPlayer(string partialName);

        /// <summary>
        /// Finds any number of offline players given a partial name (wildcards accepted)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        IEnumerable<IPlayer> FindPlayers(string partialName);

        #endregion

        #region Online Players

        /// <summary>
        /// Gets an online player given their unique ID
        /// </summary>
        /// <param name="uniqueID"></param>
        /// <returns></returns>
        ILivePlayer GetOnlinePlayer(string uniqueID);

        /// <summary>
        /// Gets all online players
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILivePlayer> GetAllOnlinePlayers();

        /// <summary>
        /// Gets all online players
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILivePlayer> Online { get; }

        /// <summary>
        /// Finds a single online player given a partial name (wildcards accepted, multiple matches returns null)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        ILivePlayer FindOnlinePlayer(string partialName);

        /// <summary>
        /// Finds any number of online players given a partial name (wildcards accepted)
        /// </summary>
        /// <param name="partialName"></param>
        /// <returns></returns>
        IEnumerable<ILivePlayer> FindOnlinePlayers(string partialName);

        #endregion

    }
}
