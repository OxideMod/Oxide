using System;
using CodeHatch.Engine.Networking;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    class ReignOfKingsPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission _libPerms;
        private readonly ulong steamid;

        /// <summary>
        /// Gets the last-known nickname for this player
        /// </summary>
        public string Nickname { get; private set; }

        /// <summary>
        /// Gets a unique ID for this player (unique within the current game)
        /// </summary>
        public string UniqueID { get; private set; }

        /// <summary>
        /// Gets the live player if this player is connected
        /// </summary>
        public ILivePlayer ConnectedPlayer => ReignOfKingsCovalenceProvider.Instance.PlayerManager.GetOnlinePlayer(UniqueID);

        internal ReignOfKingsPlayer(ulong steamID, string nickname)
        {
            if (_libPerms == null) _libPerms = Interface.Oxide.GetLibrary<Permission>();

            Nickname = nickname;
            steamid = steamID;
            UniqueID = steamID.ToString();
        }

        /// <summary>
        /// Gets if this player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm)
        {
            return _libPerms.UserHasPermission(UniqueID, perm);
        }

        /// <summary>
        /// Grants the specified permission on this user
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm)
        {
            _libPerms.GrantUserPermission(UniqueID, perm, null);
        }

        /// <summary>
        /// Strips the specified permission from this user
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm)
        {
            _libPerms.RevokeUserPermission(UniqueID, perm);
        }

        /// <summary>
        /// Gets if this player belongs to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string groupName)
        {
            return _libPerms.UserHasGroup(UniqueID, groupName);
        }

        /// <summary>
        /// Adds this player to the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void AddToGroup(string groupName)
        {
            _libPerms.AddUserGroup(UniqueID, groupName);
        }

        /// <summary>
        /// Removes this player from the specified usergroup
        /// </summary>
        /// <param name="groupName"></param>
        public void RemoveFromGroup(string groupName)
        {
            _libPerms.RemoveUserGroup(UniqueID, groupName);
        }

        public void Ban(string reason, TimeSpan duration)
        {
            Server.Ban(steamid, (int)duration.TotalSeconds, reason);
        }

        public void Unban()
        {
            Server.Unban(steamid);
        }

        public bool IsBanned => Server.IdIsBanned(steamid);

        public TimeSpan BanTimeRemaining => new DateTime(Server.GetBannedPlayerFromPlayerId(steamid).ExpireDate) - DateTime.Now;

        public bool Equals(IPlayer other)
        {
            return UniqueID == other.UniqueID;
        }
    }
}
