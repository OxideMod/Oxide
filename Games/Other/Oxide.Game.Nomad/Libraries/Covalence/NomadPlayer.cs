using System;
using System.IO;
using System.Reflection;

using TNet;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Nomad.Libraries.Covalence
{
    /// <summary>
    /// Represents a player, either connected or not
    /// </summary>
    public class NomadPlayer : IPlayer, IEquatable<IPlayer>
    {
        private static Permission libPerms;

        /// <summary>
        /// Gets/sets the name for this player
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the ID for this player (unique within the current game)
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the live player if this player is connected
        /// </summary>
        public ILivePlayer ConnectedPlayer => NomadCovalenceProvider.Instance.PlayerManager.GetOnlinePlayer(Id);

        internal NomadPlayer(string id, string name)
        {
            // Get perms library
            if (libPerms == null) libPerms = Interface.Oxide.GetLibrary<Permission>();

            // Store user details
            Name = name;
            Id = id;
        }

        #region Permissions

        /// <summary>
        /// Gets if this player has the specified permission
        /// </summary>
        /// <param name="perm"></param>
        /// <returns></returns>
        public bool HasPermission(string perm) => libPerms.UserHasPermission(Id, perm);

        /// <summary>
        /// Grants the specified permission on this user
        /// </summary>
        /// <param name="perm"></param>
        public void GrantPermission(string perm) => libPerms.GrantUserPermission(Id, perm, null);

        /// <summary>
        /// Strips the specified permission from this user
        /// </summary>
        /// <param name="perm"></param>
        public void RevokePermission(string perm) => libPerms.RevokeUserPermission(Id, perm);

        /// <summary>
        /// Gets if this player belongs to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool BelongsToGroup(string group) => libPerms.UserHasGroup(Id, group);

        /// <summary>
        /// Adds this player to the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        public void AddToGroup(string group) => libPerms.AddUserGroup(Id, group);

        /// <summary>
        /// Removes this player from the specified usergroup
        /// </summary>
        /// <param name="group"></param>
        public void RemoveFromGroup(string group) => libPerms.RemoveUserGroup(Id, group);

        #endregion

        #region Administration

        private readonly MemberInfo[] banList = typeof(GameServer).GetMember("mBan", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo saveList = typeof(Tools).GetMethod("SaveList", BindingFlags.NonPublic | BindingFlags.Static);

        public void Ban(string reason, TimeSpan duration)
        {
            // TODO
        }

        public void Unban()
        {
            // TODO

            /*var path = "Config/ban.txt";
            if (list.size <= 0)
            {
                Tools.DeleteFile(path);
                return;
            }
            path = Tools.GetDocumentsPath(path);
            var directoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryName))  Directory.CreateDirectory(directoryName);
            var streamWriter = new StreamWriter(path, false);
            for (var i = 0; i < list.size; i++) streamWriter.WriteLine(list[i]);
            streamWriter.Close();*/
        }

        public bool IsBanned => false; // TODO

        public TimeSpan BanTimeRemaining => TimeSpan.Zero; // TODO

        #endregion

        #region Chat and Commands

        public void Reply(string message, params object[] args) => ConnectedPlayer.Reply(message, args);

        #endregion

        #region Operator Overloads

        public bool Equals(IPlayer other) => Id == other.Id;

        public override int GetHashCode() => Id.GetHashCode();

        #endregion
    }
}
