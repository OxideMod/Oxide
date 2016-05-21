﻿using System;
using System.Collections.Generic;
using System.Linq;

using ProtoBuf;

using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// Contains all data for a specified user
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class UserData
    {
        /// <summary>
        /// Gets or sets the last seen nickname for this user
        /// </summary>
        public string LastSeenNickname { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the individual permissions for this user
        /// </summary>
        public HashSet<string> Perms { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the usergroup for this user
        /// </summary>
        public HashSet<string> Groups { get; set; } = new HashSet<string>();
    }

    /// <summary>
    /// Contains all data for a specified group
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class GroupData
    {
        /// <summary>
        /// Gets or sets the title of this group
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rank of this group
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Gets or sets the individual permissions for this group
        /// </summary>
        public HashSet<string> Perms { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the parent for this group
        /// </summary>
        public string ParentGroup { get; set; } = string.Empty;
    }

    /// <summary>
    /// A library providing a unified permissions system
    /// </summary>
    public class Permission : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        // All registered permissions
        private readonly Dictionary<Plugin, HashSet<string>> permset;

        // All user data
        private Dictionary<string, UserData> userdata;

        // All group data
        private Dictionary<string, GroupData> groupdata;

        private Func<string, bool> validate;

        // Permission status
        public bool IsLoaded { get; private set; }

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
            Utility.DatafileToProto<Dictionary<string, UserData>>("oxide.users");
            Utility.DatafileToProto<Dictionary<string, GroupData>>("oxide.groups");
            userdata = ProtoStorage.Load<Dictionary<string, UserData>>("oxide.users") ?? new Dictionary<string, UserData>();
            groupdata = ProtoStorage.Load<Dictionary<string, GroupData>>("oxide.groups") ?? new Dictionary<string, GroupData>();
            foreach (var pair in groupdata)
            {
                if (!string.IsNullOrEmpty(pair.Value.ParentGroup) && HasCircularParent(pair.Key, pair.Value.ParentGroup))
                {
                    Interface.Oxide.LogWarning("Detected circular parent group for '{0}'! Removing parent '{1}'", pair.Key, pair.Value.ParentGroup);
                    pair.Value.ParentGroup = null;
                }
            }
            IsLoaded = true;
        }

        /// <summary>
        /// Exports user/group data to json
        /// </summary>
        [LibraryFunction("Export")]
        public void Export(string prefix = "auth")
        {
            if (!IsLoaded) return;
            Interface.Oxide.DataFileSystem.WriteObject(prefix + ".groups", groupdata);
            Interface.Oxide.DataFileSystem.WriteObject(prefix + ".users", userdata);
        }

        /// <summary>
        /// Saves all permissions data to the datafile
        /// </summary>
        private void SaveUsers() => ProtoStorage.Save(userdata, "oxide.users");

        /// <summary>
        /// Saves all permissions data to the datafile
        /// </summary>
        private void SaveGroups() => ProtoStorage.Save(groupdata, "oxide.groups");

        /// <summary>
        /// Register user id validation
        /// </summary>
        public void RegisterValidate(Func<string, bool> validate)
        {
            this.validate = validate;
        }

        /// <summary>
        /// Clean invalid user id entries
        /// </summary>
        public void CleanUp()
        {
            if (!IsLoaded || validate == null) return;
            var invalid = userdata.Keys.Where(k => !validate(k)).ToArray();
            if (invalid.Length <= 0) return;
            foreach (var i in invalid) userdata.Remove(i);
            SaveUsers();
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
            if (string.IsNullOrEmpty(name)) return;
            name = name.ToLower();

            if (PermissionExists(name))
            {
                Interface.Oxide.LogWarning("Duplicate permission registered '{0}' (by plugin '{1}')", name, owner.Title);
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

            var prefix = owner.Name.ToLower() + ".";
            if (!name.StartsWith(prefix))
                Interface.Oxide.LogWarning("Missing plugin name prefix '{0}' for permission '{1}' (by plugin '{2}')", prefix, name, owner.Title);
        }

        /// <summary>
        /// Returns if the specified permission exists or not
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        [LibraryFunction("PermissionExists")]
        public bool PermissionExists(string name, Plugin owner = null)
        {
            if (string.IsNullOrEmpty(name)) return false;
            name = name.ToLower();

            if (owner == null)
            {
                if (permset.Count > 0)
                {
                    if (name.Equals("*")) return true;
                    if (name.EndsWith("*"))
                    {
                        name = name.TrimEnd('*');
                        return permset.Values.SelectMany(v => v).Any(p => p.StartsWith(name));
                    }
                }
                return permset.Values.Any(v => v.Contains(name));
            }

            HashSet<string> set;
            if (!permset.TryGetValue(owner, out set)) return false;
            if (set.Count > 0)
            {
                if (name.Equals("*")) return true;
                if (name.EndsWith("*"))
                {
                    name = name.TrimEnd('*');
                    return set.Any(p => p.StartsWith(name));
                }
            }
            return set.Contains(name);
        }

        #endregion

        /// <summary>
        /// Called when a plugin has been unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void owner_OnRemovedFromManager(Plugin sender, PluginManager manager) => permset.Remove(sender);

        #region Querying

        /// <summary>
        /// Returns if the specified user id is valid
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        [LibraryFunction("UserIdValid")]
        public bool UserIdValid(string userid) => validate == null || validate(userid);

        /// <summary>
        /// Returns if the specified user exists
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        [LibraryFunction("UserExists")]
        public bool UserExists(string userid) => userdata.ContainsKey(userid);

        /// <summary>
        /// Returns the data for the specified user
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        private UserData GetUserData(string userid)
        {
            UserData data;
            if (!userdata.TryGetValue(userid, out data)) userdata.Add(userid, data = new UserData());

            // Return the data
            return data;
        }

        /// <summary>
        /// Updates the nickname
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="nickname"></param>
        [LibraryFunction("UpdateNickname")]
        public void UpdateNickname(string userid, string nickname)
        {
            if (UserExists(userid)) GetUserData(userid).LastSeenNickname = nickname;
        }

        /// <summary>
        /// Check if user has a group
        /// </summary>
        /// <param name="userid"></param>
        [LibraryFunction("UserHasAnyGroup")]
        public bool UserHasAnyGroup(string userid)
        {
            if (!UserExists(userid)) return false;
            return GetUserData(userid).Groups.Count > 0;
        }

        /// <summary>
        /// Returns if the specified group has the specified permission or not
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="perm"></param>
        /// <returns></returns>
        [LibraryFunction("GroupsHavePermission")]
        public bool GroupsHavePermission(HashSet<string> groups, string perm) => groups.Any(@group => GroupHasPermission(@group, perm));

        /// <summary>
        /// Returns if the specified group has the specified permission or not
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="perm"></param>
        /// <returns></returns>
        [LibraryFunction("GroupHasPermission")]
        public bool GroupHasPermission(string groupname, string perm)
        {
            if (!GroupExists(groupname) || string.IsNullOrEmpty(perm)) return false;
            // Check if the group has the perm

            GroupData group;
            if (!groupdata.TryGetValue(groupname.ToLower(), out group)) return false;
            return group.Perms.Contains(perm.ToLower()) || GroupHasPermission(group.ParentGroup, perm);
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
            if (string.IsNullOrEmpty(perm)) return false;
            perm = perm.ToLower();

            // First, get the user data
            var data = GetUserData(userid);

            // Check if they have the perm
            if (data.Perms.Contains(perm)) return true;

            // Check if their group has the perm
            return GroupsHavePermission(data.Groups, perm);
        }

        /// <summary>
        /// Returns the group to which the specified user belongs
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        [LibraryFunction("GetUserGroups")]
        public string[] GetUserGroups(string userid)
        {
            // First, get the user data
            var data = GetUserData(userid);

            // Return the group
            return data.Groups.ToArray();
        }

        /// <summary>
        /// Returns the permissions which the specified user has
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        [LibraryFunction("GetUserPermissions")]
        public string[] GetUserPermissions(string userid)
        {
            // First, get the user data
            var data = GetUserData(userid);

            var perms = data.Perms.ToList();
            foreach (var @group in data.Groups) perms.AddRange(GetGroupPermissions(@group));
            return new HashSet<string>(perms).ToArray();
        }

        /// <summary>
        /// Returns the permissions which the specified group has
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("GetGroupPermissions")]
        public string[] GetGroupPermissions(string groupname)
        {
            if (!GroupExists(groupname)) return new string[0];

            GroupData group;
            if (!groupdata.TryGetValue(groupname.ToLower(), out group)) return new string[0];

            var perms = group.Perms.ToList();
            perms.AddRange(GetGroupPermissions(group.ParentGroup));
            return new HashSet<string>(perms).ToArray();
        }

        /// <summary>
        /// Returns the permissions which are registered
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetPermissions")]
        public string[] GetPermissions() => new HashSet<string>(permset.Values.SelectMany(v => v)).ToArray();

        /// <summary>
        /// Returns the users with given permission
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetPermissionUsers")]
        public string[] GetPermissionUsers(string perm)
        {
            if (string.IsNullOrEmpty(perm)) return new string[0];
            perm = perm.ToLower();
            var users = new HashSet<string>();
            foreach (var data in userdata)
                if (data.Value.Perms.Contains(perm))
                    users.Add($"{data.Key}({data.Value.LastSeenNickname})");
            return users.ToArray();
        }

        /// <summary>
        /// Returns the groups with given permission
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetPermissionGroups")]
        public string[] GetPermissionGroups(string perm)
        {
            if (string.IsNullOrEmpty(perm)) return new string[0];
            perm = perm.ToLower();
            var groups = new HashSet<string>();
            foreach (var data in groupdata)
                if (data.Value.Perms.Contains(perm))
                    groups.Add(data.Key);
            return groups.ToArray();
        }

        /// <summary>
        /// Set the group to which the specified user belongs
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("AddUserGroup")]
        public void AddUserGroup(string userid, string groupname)
        {
            if (!GroupExists(groupname)) return;

            var data = GetUserData(userid);
            if (!data.Groups.Add(groupname.ToLower())) return;
            SaveUsers();
        }

        /// <summary>
        /// Set the group to which the specified user belongs
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("RemoveUserGroup")]
        public void RemoveUserGroup(string userid, string groupname)
        {
            if (!GroupExists(groupname)) return;

            var data = GetUserData(userid);
            if (groupname.Equals("*"))
            {
                if (data.Groups.Count <= 0) return;
                data.Groups.Clear();
                SaveUsers();
                return;
            }
            if (!data.Groups.Remove(groupname.ToLower())) return;
            SaveUsers();
        }

        /// <summary>
        /// Get if the user belongs to given group
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("UserHasGroup")]
        public bool UserHasGroup(string userid, string groupname)
        {
            if (!GroupExists(groupname)) return false;

            var data = GetUserData(userid);
            return data.Groups.Contains(groupname.ToLower());
        }

        /// <summary>
        /// Returns if the specified group exists or not
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("GroupExists")]
        public bool GroupExists(string groupname)
        {
            return !string.IsNullOrEmpty(groupname) && (groupname.Equals("*") || groupdata.ContainsKey(groupname.ToLower()));
        }

        /// <summary>
        /// Returns existing groups
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetGroups")]
        public string[] GetGroups() => groupdata.Keys.ToArray();

        /// <summary>
        /// Returns users in that group
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("GetUsersInGroup")]
        public string[] GetUsersInGroup(string groupname)
        {
            if (!GroupExists(groupname)) return new string[0];
            groupname = groupname.ToLower();
            return userdata.Where(u => u.Value.Groups.Contains(groupname)).Select(u => $"{u.Key}({u.Value.LastSeenNickname})").ToArray();
        }

        /// <summary>
        /// Returns the rank of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        [LibraryFunction("GetGroupRank")]
        public int GetGroupRank(string groupname)
        {
            if (!GroupExists(groupname)) return 0;

            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname.ToLower(), out data)) return 0;

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

            perm = perm.ToLower();

            if (perm.EndsWith("*"))
            {
                HashSet<string> perms;
                if (owner == null)
                    perms = new HashSet<string>(permset.Values.SelectMany(v => v));
                else if (!permset.TryGetValue(owner, out perms))
                    return;
                if (perm.Equals("*"))
                {
                    if (!perms.Aggregate(false, (c, s) => c | data.Perms.Add(s))) return;
                }
                else
                {
                    perm = perm.TrimEnd('*');
                    if (!perms.Where(s => s.StartsWith(perm)).Aggregate(false, (c, s) => c | data.Perms.Add(s))) return;
                }
                SaveUsers();
                return;
            }

            // Add the perm and save
            if (!data.Perms.Add(perm)) return;
            SaveUsers();

            // Call hook for plugins
            Interface.CallHook("OnUserPermissionGranted", userid, perm);
        }

        /// <summary>
        /// Revokes the specified permission from the specified user
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="perm"></param>
        [LibraryFunction("RevokeUserPermission")]
        public void RevokeUserPermission(string userid, string perm)
        {
            if (string.IsNullOrEmpty(perm)) return;

            // Get the user data
            var data = GetUserData(userid);

            if (perm.Equals("*"))
            {
                if (data.Perms.Count <= 0) return;
                data.Perms.Clear();
                SaveUsers();
                return;
            }

            // Remove the perm and save
            if (!data.Perms.Remove(perm.ToLower())) return;
            SaveUsers();

            // Call hook for plugins
            Interface.CallHook("OnUserPermissionRevoked", userid, perm);
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
            if (!PermissionExists(perm, owner) || !GroupExists(groupname)) return;

            // Get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname.ToLower(), out data)) return;

            if (perm.EndsWith("*"))
            {
                HashSet<string> perms;
                if (owner == null)
                    perms = new HashSet<string>(permset.Values.SelectMany(v => v));
                else if (!permset.TryGetValue(owner, out perms))
                    return;
                if (perm.Equals("*"))
                {
                    if (!perms.Aggregate(false, (c, s) => c | data.Perms.Add(s))) return;
                }
                else
                {
                    perm = perm.TrimEnd('*');
                    if (!perms.Where(s => s.StartsWith(perm)).Aggregate(false, (c, s) => c | data.Perms.Add(s))) return;
                }
                SaveGroups();
                return;
            }

            // Add the perm and save
            if (!data.Perms.Add(perm.ToLower())) return;
            SaveGroups();

            // Call hook for plugins
            Interface.CallHook("OnGroupPermissionGranted", groupname, perm);
        }

        /// <summary>
        /// Revokes the specified permission from the specified user
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="perm"></param>
        [LibraryFunction("RevokeGroupPermission")]
        public void RevokeGroupPermission(string groupname, string perm)
        {
            if (!GroupExists(groupname) || string.IsNullOrEmpty(perm)) return;

            // Get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname.ToLower(), out data)) return;

            if (perm.Equals("*"))
            {
                if (data.Perms.Count <= 0) return;
                data.Perms.Clear();
                SaveGroups();
                return;
            }

            // Remove the perm and save
            if (!data.Perms.Remove(perm.ToLower())) return;
            SaveGroups();

            // Call hook for plugins
            Interface.CallHook("OnGroupPermissionRevoked", groupname, perm);
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
        public bool CreateGroup(string name, string title, int rank)
        {
            // Check if it already exists
            if (GroupExists(name) || string.IsNullOrEmpty(name)) return false;

            // Create the data
            var data = new GroupData { Title = title, Rank = rank };

            // Add it and save
            groupdata.Add(name.ToLower(), data);
            SaveGroups();
            return true;
        }

        /// <summary>
        /// Removes the specified group
        /// </summary>
        /// <param name="name"></param>
        [LibraryFunction("RemoveGroup")]
        public bool RemoveGroup(string name)
        {
            // Check if it even exists
            if (!GroupExists(name)) return false;
            name = name.ToLower();

            // Remove and save
            groupdata.Remove(name);

            // Remove group from users
            var changed = userdata.Values.Aggregate(false, (current, userData) => current | userData.Groups.Remove(name));

            if (changed) SaveUsers();
            SaveGroups();
            return true;
        }

        /// <summary>
        /// Sets the title of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="title"></param>
        [LibraryFunction("SetGroupTitle")]
        public bool SetGroupTitle(string groupname, string title)
        {
            if (!GroupExists(groupname)) return false;

            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname.ToLower(), out data)) return false;

            // Change and save
            if (data.Title == title) return true;
            data.Title = title;
            SaveGroups();
            return true;
        }

        /// <summary>
        /// Sets the rank of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="rank"></param>
        [LibraryFunction("SetGroupRank")]
        public bool SetGroupRank(string groupname, int rank)
        {
            if (!GroupExists(groupname)) return false;
            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname.ToLower(), out data)) return false;

            // Change and save
            if (data.Rank == rank) return true;
            data.Rank = rank;
            SaveGroups();
            return true;
        }

        /// <summary>
        /// Gets the parent of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        [LibraryFunction("GetGroupParent")]
        public string GetGroupParent(string groupname)
        {
            if (!GroupExists(groupname)) return string.Empty;
            groupname = groupname.ToLower();

            GroupData data;
            return !groupdata.TryGetValue(groupname, out data) ? string.Empty : data.ParentGroup;
        }

        /// <summary>
        /// Sets the parent of the specified group
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="parent"></param>
        [LibraryFunction("SetGroupParent")]
        public bool SetGroupParent(string groupname, string parent)
        {
            if (!GroupExists(groupname)) return false;
            groupname = groupname.ToLower();

            // First, get the group data
            GroupData data;
            if (!groupdata.TryGetValue(groupname, out data)) return false;

            if (string.IsNullOrEmpty(parent))
            {
                data.ParentGroup = null;
                SaveGroups();
                return true;
            }

            if (!GroupExists(parent) || groupname.Equals(parent.ToLower())) return false;
            parent = parent.ToLower();

            if (!string.IsNullOrEmpty(data.ParentGroup) && data.ParentGroup.Equals(parent)) return true;
            if (HasCircularParent(groupname, parent)) return false;

            // Change and save
            data.ParentGroup = parent;
            SaveGroups();
            return true;
        }

        private bool HasCircularParent(string groupname, string parent)
        {
            // Get parent data
            GroupData parentData;

            if (!groupdata.TryGetValue(parent, out parentData)) return false;

            // Check for circular reference
            var groups = new HashSet<string> { groupname, parent };
            while (!string.IsNullOrEmpty(parentData.ParentGroup))
            {
                // Found itself?
                if (!groups.Add(parentData.ParentGroup)) return true;

                // Get next parent
                if (!groupdata.TryGetValue(parentData.ParentGroup, out parentData)) return false;
            }
            return false;
        }

        #endregion
    }
}
