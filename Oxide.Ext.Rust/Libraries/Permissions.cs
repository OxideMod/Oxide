using System.Collections.Generic;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;

namespace Oxide.Rust.Libraries
{
    /// <summary>
    /// Represents the unique identity of a user
    /// </summary>
    public struct UserIdentity
    {
        public ulong UID;
    }

    /// <summary>
    /// Contains all data for a specified user
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// Gets or sets the last seen nickname for this user
        /// </summary>
        public string LastSeenNickname { get; set; }

        /// <summary>
        /// Gets or sets the individual permissions for this user
        /// </summary>
        public HashSet<string> Perms { get; set; }

        /// <summary>
        /// Gets or sets the usergroup for this user
        /// </summary>
        public string Usergroup { get; set; }
    }

    /// <summary>
    /// Contains all data for a specified group
    /// </summary>
    public class GroupData
    {
        /// <summary>
        /// Gets or sets the title of this group
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the rank of this group
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Gets or sets the individual permissions for this group
        /// </summary>
        public HashSet<string> Perms { get; set; }
    }

    /// <summary>
    /// A library providing a unified permissions system
    /// </summary>
    public class Permissions : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        // All registered permissions
        private Dictionary<Plugin, HashSet<string>> permset;
        private HashSet<string> allperms;

        // All user data
        private Dictionary<UserIdentity, UserData> userdata;

        // All group data
        private Dictionary<string, GroupData> groupdata;

        // The datafile
        private DynamicConfigFile datafile;

        /// <summary>
        /// Initializes a new instance of the Permissions library
        /// </summary>
        public Permissions()
        {
            // Initialize
            permset = new Dictionary<Plugin, HashSet<string>>();
            allperms = new HashSet<string>();

            // Load the datafile
            datafile = Interface.GetMod().DataFileSystem.GetDatafile("oxide.permissions");
            LoadFromDatafile();
        }

        /// <summary>
        /// Loads all permissions data from the datafile
        /// </summary>
        private void LoadFromDatafile()
        {
            // Initialize
            userdata = new Dictionary<UserIdentity, UserData>();
            groupdata = new Dictionary<string, GroupData>();

            // Load all groups
            var grouplist = datafile["groups"] as Dictionary<string, object>;
            if (grouplist != null)
            {
                foreach (string name in grouplist.Keys)
                {
                    object obj = grouplist[name];
                    var groupinfo = obj as Dictionary<string, object>;
                    if (groupinfo != null)
                    {
                        GroupData data = new GroupData();
                        object tmp;
                        if (groupinfo.TryGetValue("Rank", out tmp) && tmp is int)
                            data.Rank = (int)tmp;
                        if (groupinfo.TryGetValue("Title", out tmp) && tmp is string)
                            data.Title = tmp as string;
                        else
                            data.Title = string.Format("Untitled Group #{0}", groupdata.Count);
                        data.Perms = new HashSet<string>();
                        if (groupinfo.TryGetValue("Perms", out tmp) && tmp is List<object>)
                        {
                            var permlist = tmp as List<object>;
                            foreach (var permobj in permlist)
                                if (permobj is string)
                                    data.Perms.Add(permobj as string);
                        }
                        groupdata.Add(name, data);
                    }
                }
            }

            // Load all users
            var userlist = datafile["users"] as Dictionary<string, object>;
            if (userlist != null)
            {
                foreach (string struid in userlist.Keys)
                {
                    ulong uid;
                    if (ulong.TryParse(struid, out uid))
                    {
                        UserIdentity identity = new UserIdentity { UID = uid };
                        object obj = userlist[struid];
                        var userinfo = obj as Dictionary<string, object>;
                        if (userinfo != null)
                        {
                            UserData data = new UserData();
                            object tmp;
                            if (userinfo.TryGetValue("LastSeenNickname", out tmp) && tmp is string)
                                data.LastSeenNickname = tmp as string;
                            else
                                data.LastSeenNickname = string.Empty;
                            if (userinfo.TryGetValue("Usergroup", out tmp) && tmp is string)
                                data.Usergroup = tmp as string;
                            else
                                data.Usergroup = string.Empty;
                            data.Perms = new HashSet<string>();
                            if (userinfo.TryGetValue("Perms", out tmp) && tmp is List<object>)
                            {
                                var permlist = tmp as List<object>;
                                foreach (var permobj in permlist)
                                    if (permobj is string)
                                        data.Perms.Add(permobj as string);
                            }
                            userdata.Add(identity, data);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves all permissions data to the datafile
        /// </summary>
        private void SaveToDatafile()
        {
            datafile.Clear();
            Dictionary<string, object> grouplist = new Dictionary<string, object>();
            Dictionary<string, object> userlist = new Dictionary<string, object>();
            datafile["groups"] = grouplist;
            datafile["users"] = userlist;
            foreach (var pair in groupdata)
            {
                grouplist.Add(pair.Key, new Dictionary<string, object>()
                {
                    { "Title", pair.Value.Title },
                    { "Rank", pair.Value.Rank },
                    { "Perms", new List<object>(pair.Value.Perms.Cast<object>()) }
                });
            }
            foreach (var pair in userdata)
            {
                userlist.Add(pair.Key.UID.ToString(), new Dictionary<string, object>()
                {
                    { "LastSeenNickname", pair.Value.LastSeenNickname },
                    { "Usergroup", pair.Value.Usergroup },
                    { "Perms", new List<object>(pair.Value.Perms.Cast<object>()) }
                });
            }
            Interface.GetMod().DataFileSystem.SaveDatafile("oxide.permissions");
        }

        #region Permission Management

        /// <summary>
        /// Registers the specified permission
        /// </summary>
        /// <param name="name"></param>
        [LibraryFunction("RegisterPermission")]
        public void RegisterPermission(string name, Plugin owner)
        {
            if (PermissionExists(name))
            {
                Interface.GetMod().RootLogger.Write(LogType.Warning, "Duplicate permission registered '{0}' (by plugin '{1}')", name, owner.Title);
                return;
            }
            HashSet<string> set;
            if (!permset.TryGetValue(owner, out set))
            {
                set = new HashSet<string>();
                permset.Add(owner, set);
                owner.OnRemovedFromManager += owner_OnRemovedFromManager;
            }
            set.Add(name);
            allperms.Add(name);
        }

        /// <summary>
        /// Returns if the specified permission exists or not
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("PermissionExists")]
        public bool PermissionExists(string name)
        {
            return allperms.Contains(name);
        }

        #endregion

        /// <summary>
        /// Called when a plugin has been unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            permset.Remove(sender);
        }

        #region Identities

        /// <summary>
        /// Returns the identity for the specified player
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [LibraryFunction("GetIdentityFromPlayer")]
        public UserIdentity GetIdentityFromPlayer(BasePlayer player)
        {
            return new UserIdentity { UID = player.userID };
        }

        /// <summary>
        /// Returns the identity for the specified connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [LibraryFunction("GetIdentityFromConnection")]
        public UserIdentity GetIdentityFromPlayer(Network.Connection connection)
        {
            return new UserIdentity { UID = connection.userid };
        }

        #endregion

        #region Querying

        /// <summary>
        /// Returns if the specified group has the specified permission or not
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="perm"></param>
        /// <returns></returns>
        [LibraryFunction("GroupHasPermission")]
        public bool GroupHasPermission(string groupname, string perm)
        {
            // Check if the group has the perm
            GroupData group;
            if (!groupdata.TryGetValue(groupname, out group)) return false;
            return group.Perms.Contains(perm);
        }

        /// <summary>
        /// Returns if the specified user has the specified permission
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        [LibraryFunction("UserHasPermission")]
        public bool UserHasPermission(UserIdentity user, string perm)
        {
            // First, get the user data
            UserData data;
            if (!userdata.TryGetValue(user, out data)) return false;

            // Check if they have the perm
            if (data.Perms.Contains(perm)) return true;

            // Check if their group has the perm
            return GroupHasPermission(data.Usergroup, perm);
        }

        /// <summary>
        /// Returns the group to which the specified user belongs
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [LibraryFunction("GetUserGroup")]
        public string GetUserGroup(UserIdentity user)
        {
            // First, get the user data
            UserData data;
            if (!userdata.TryGetValue(user, out data)) return string.Empty;

            // Return the group
            return data.Usergroup;
        }

        /// <summary>
        /// Returns if the specified group exists or not
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("GroupExists")]
        public bool GroupExists(string groupname)
        {
            return groupdata.ContainsKey(groupname);
        }

        /// <summary>
        /// Returns the rank of the specified group
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [LibraryFunction("GetGroupRank")]
        public int GetGroupRank(string groupname)
        {
            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return 0;

            // Return the group
            return data.Rank;
        }

        #endregion

        /// <summary>
        /// Registers that the specified player has been "seen"
        /// </summary>
        /// <param name="player"></param>
        internal void RegisterUser(BasePlayer player)
        {
            // Get the identity
            UserIdentity user = GetIdentityFromPlayer(player);

            // Get the user data
            UserData data;
            if (!userdata.TryGetValue(user, out data))
            {
                // Create the record
                data = new UserData();
                data.LastSeenNickname = player.name;
                data.Usergroup = string.Empty;
                data.Perms = new HashSet<string>();
                userdata.Add(user, data);

                // Call the hook
                Interface.CallHook("OnNewUser", new object[] { player, user });
            }
            else
            {
                // Edit the record
                if (data.LastSeenNickname == player.name) return;
                data.LastSeenNickname = player.name;
            }

            // Save
            SaveToDatafile();
        }

        #region User Permissions

        /// <summary>
        /// Grants the specified permission to the specified user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="perm"></param>
        [LibraryFunction("GrantUserPermission")]
        public void GrantUserPermission(UserIdentity user, string perm)
        {
            // Check it's even a perm
            if (!PermissionExists(perm)) return;

            // Get the user data
            UserData data;
            if (!userdata.TryGetValue(user, out data)) return;

            // Add the perm and save
            if (data.Perms.Contains(perm)) return;
            data.Perms.Add(perm);
            SaveToDatafile();
        }

        /// <summary>
        /// Revokes the specified permission from the specified user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="perm"></param>
        [LibraryFunction("RevokeUserPermission")]
        public void RevokeUserPermission(UserIdentity user, string perm)
        {
            // Get the user data
            UserData data;
            if (!userdata.TryGetValue(user, out data)) return;

            // Remove the perm and save
            if (!data.Perms.Contains(perm)) return;
            data.Perms.Remove(perm);
            SaveToDatafile();
        }

        #endregion

        #region Group Permissions

        /// <summary>
        /// Grant the specified permission to the specified group
        /// </summary>
        /// <param name="user"></param>
        /// <param name="perm"></param>
        [LibraryFunction("GrantGroupPermission")]
        public void GrantGroupPermission(string groupname, string perm)
        {
            // Check it's even a perm
            if (!PermissionExists(perm)) return;

            // Get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return;

            // Add the perm and save
            if (data.Perms.Contains(perm)) return;
            data.Perms.Add(perm);
            SaveToDatafile();
        }

        /// <summary>
        /// Revokes the specified permission from the specified user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="perm"></param>
        [LibraryFunction("RevokeGroupPermission")]
        public void RevokeGroupPermission(string groupname, string perm)
        {
            // Get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return;

            // Remove the perm and save
            if (!data.Perms.Contains(perm)) return;
            data.Perms.Remove(perm);
            SaveToDatafile();
        }

        #endregion

        #region Group Management

        /// <summary>
        /// Creates the specified group
        /// </summary>
        /// <param name="name"></param>
        /// <param name="title"></param>
        /// <param name="rank"></param>
        [LibraryFunction("CreateGroup")]
        public void CreateGroup(string name, string title, int rank)
        {
            // Check if it already exists
            if (groupdata.ContainsKey(name)) return;

            // Create the data
            GroupData data = new GroupData();
            data.Title = title;
            data.Rank = rank;
            data.Perms = new HashSet<string>();

            // Add it and save
            groupdata.Add(name, data);
            SaveToDatafile();
        }

        /// <summary>
        /// Removes the specified group
        /// </summary>
        /// <param name="name"></param>
        [LibraryFunction("RemoveGroup")]
        public void RemoveGroup(string name)
        {
            // Check if it even exists
            if (!groupdata.ContainsKey(name)) return;

            // Remove and save
            groupdata.Remove(name);
            SaveToDatafile();
        }

        /// <summary>
        /// Sets the title of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="title"></param>
        [LibraryFunction("SetGroupTitle")]
        public void SetGroupTitle(string groupname, string title)
        {
            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return;

            // Change and save
            if (data.Title == title) return;
            data.Title = title;
            SaveToDatafile();
        }

        /// <summary>
        /// Sets the rank of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="title"></param>
        [LibraryFunction("SetGroupRank")]
        public void SetGroupRank(string groupname, int rank)
        {
            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return;

            // Change and save
            if (data.Rank == rank) return;
            data.Rank = rank;
            SaveToDatafile();
        }

        #endregion
    }
}
