using System;
using System.Collections.Generic;

using Oxide.Core.Plugins;
using Oxide.Core.Logging;

namespace Oxide.Core.Libraries
{
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
    public class Permission : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        // All registered permissions
        private readonly Dictionary<Plugin, HashSet<string>> permset;

        // All user data
        private Dictionary<string, UserData> userdata;

        // All group data
        private Dictionary<string, GroupData> groupdata;

        /// <summary>
        /// Initializes a new instance of the Permission library
        /// </summary>
        public Permission()
        {
            // Initialize
            permset = new Dictionary<Plugin, HashSet<string>>();

            // Load the datafile
            LoadFromDatafile();
        }

        /// <summary>
        /// Loads all permissions data from the datafile
        /// </summary>
        private void LoadFromDatafile()
        {
            // Initialize
            try
            {
                userdata = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, UserData>>("oxide.users");
            }
            catch (Exception)
            {
                userdata = new Dictionary<string, UserData>();
            }
            try
            {
                groupdata = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, GroupData>>("oxide.groups");
            }
            catch (Exception)
            {
                groupdata = new Dictionary<string, GroupData>();
            }
        }

        /// <summary>
        /// Saves all permissions data to the datafile
        /// </summary>
        private void SaveUsers()
        {
            Interface.GetMod().DataFileSystem.WriteObject("oxide.users", userdata);
        }

        /// <summary>
        /// Saves all permissions data to the datafile
        /// </summary>
        private void SaveGroups()
        {
            Interface.GetMod().DataFileSystem.WriteObject("oxide.groups", groupdata);
        }

        #region Permission Management

        /// <summary>
        /// Registers the specified permission
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        [LibraryFunction("RegisterPermission")]
        public void RegisterPermission(string name, Plugin owner)
        {
            if (PermissionExists(name, owner))
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
        }

        /// <summary>
        /// Returns if the specified permission exists or not
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("PermissionExists")]
        public bool PermissionExists(string name, Plugin owner)
        {
            HashSet<string> set;
            if (!permset.TryGetValue(owner, out set)) return false;
            return set.Contains(name);
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

        #region Querying

        /// <summary>
        /// Returns the data for the specified user
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("GetUserData")]
        public UserData GetUserData(string userid, string name = "")
        {
            UserData data;
            if (!userdata.TryGetValue(userid, out data))
            {
                data = new UserData {LastSeenNickname = name, Usergroup = string.Empty, Perms = new HashSet<string>()};
                userdata.Add(userid, data);
            }

            // Return the data
            return data;
        }

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
        /// <param name="userid"></param>
        /// <param name="perm"></param>
        /// <returns></returns>
        [LibraryFunction("UserHasPermission")]
        public bool UserHasPermission(string userid, string perm)
        {
            // First, get the user data
            var data = GetUserData(userid);

            // Check if they have the perm
            if (data.Perms.Contains(perm)) return true;

            // Check if their group has the perm
            return GroupHasPermission(data.Usergroup, perm);
        }

        /// <summary>
        /// Returns the group to which the specified user belongs
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        [LibraryFunction("GetUserGroup")]
        public string GetUserGroup(string userid)
        {
            // First, get the user data
            var data = GetUserData(userid);

            // Return the group
            return data.Usergroup;
        }

        /// <summary>
        /// Set the group to which the specified user belongs
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("SetUserGroup")]
        public void SetUserGroup(string userid, string groupname)
        {
            if (!GroupExists(groupname)) return;

            var data = GetUserData(userid);
            data.Usergroup = groupname;
            SaveUsers();
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
        /// <param name="groupname"></param>
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

        #region User Permission

        /// <summary>
        /// Grants the specified permission to the specified user
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="perm"></param>
        /// <param name="owner"></param>
        [LibraryFunction("GrantUserPermission")]
        public void GrantUserPermission(string userid, string perm, Plugin owner)
        {
            // Check it's even a perm
            if (!PermissionExists(perm, owner)) return;

            // Get the user data
            var data = GetUserData(userid);

            // Add the perm and save
            if (!data.Perms.Add(perm)) return;
            SaveUsers();
        }

        /// <summary>
        /// Revokes the specified permission from the specified user
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="perm"></param>
        [LibraryFunction("RevokeUserPermission")]
        public void RevokeUserPermission(string userid, string perm)
        {
            // Get the user data
            var data = GetUserData(userid);

            // Remove the perm and save
            if (!data.Perms.Remove(perm)) return;
            SaveUsers();
        }

        #endregion

        #region Group Permission

        /// <summary>
        /// Grant the specified permission to the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="perm"></param>
        /// <param name="owner"></param>
        [LibraryFunction("GrantGroupPermission")]
        public void GrantGroupPermission(string groupname, string perm, Plugin owner)
        {
            // Check it's even a perm
            if (!PermissionExists(perm, owner)) return;

            // Get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return;

            // Add the perm and save
            if (!data.Perms.Add(perm)) return;
            SaveGroups();
        }

        /// <summary>
        /// Revokes the specified permission from the specified user
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="perm"></param>
        [LibraryFunction("RevokeGroupPermission")]
        public void RevokeGroupPermission(string groupname, string perm)
        {
            // Get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return;

            // Remove the perm and save
            if (!data.Perms.Remove(perm)) return;
            SaveGroups();
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
            var data = new GroupData {Title = title, Rank = rank, Perms = new HashSet<string>()};

            // Add it and save
            groupdata.Add(name, data);
            SaveGroups();
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
            //TODO unset user groups?
            SaveGroups();
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
            SaveGroups();
        }

        /// <summary>
        /// Sets the rank of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="rank"></param>
        [LibraryFunction("SetGroupRank")]
        public void SetGroupRank(string groupname, int rank)
        {
            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return;

            // Change and save
            if (data.Rank == rank) return;
            data.Rank = rank;
            SaveGroups();
        }

        #endregion
    }
}
