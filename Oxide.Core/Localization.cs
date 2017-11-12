using System.Collections.Generic;

namespace Oxide.Core
{
    public static class Localization
    {
        public static readonly Dictionary<string, Dictionary<string, string>> languages = new Dictionary<string, Dictionary<string, string>>
        {
            #region English

            ["en"] = new Dictionary<string, string>
            {
                ["CommandUsageGrant"] = "Usage: grant <group|user> <name|id> <permission>",
                ["CommandUsageGroup"] = "Usage: group <add|set> <name> [title] [rank]",
                ["CommandUsageGroupParent"] = "Usage: group <parent> <name> <parentName>",
                ["CommandUsageGroupRemove"] = "Usage: group <remove> <name>",
                ["CommandUsageLang"] = "Usage: lang <two-digit language code>",
                ["CommandUsageLoad"] = "Usage: load *|<pluginname>+",
                ["CommandUsageReload"] = "Usage: reload *|<pluginname>+",
                ["CommandUsageRevoke"] = "Usage: revoke <group|user> <name|id> <permission>",
                ["CommandUsageShow"] = "Usage: show <groups|perms>",
                ["CommandUsageShowName"] = "Usage: show <group|user> <name>",
                ["CommandUsageUnload"] = "Usage: unload *|<pluginname>+",
                ["CommandUsageUserGroup"] = "Usage: usergroup <add|remove> <username> <groupname>",
                ["ConnectionRejected"] = "Connection was rejected",
                ["GroupAlreadyExists"] = "Group '{0}' already exists",
                ["GroupAlreadyHasPermission"] = "Group '{0}' already has permission '{1}'",
                ["GroupDoesNotHavePermission"] = "Group '{0}' does not have permission '{1}'",
                ["GroupChanged"] = "Group '{0}' changed",
                ["GroupCreated"] = "Group '{0}' created",
                ["GroupDeleted"] = "Group '{0}' deleted",
                ["GroupNotFound"] = "Group '{0}' doesn't exist",
                ["GroupParentChanged"] = "Group '{0}' parent changed to '{1}'",
                ["GroupParentNotChanged"] = "Group '{0}' parent was not changed",
                ["GroupParentNotFound"] = "Group parent '{0}' doesn't exist",
                ["GroupPermissionGranted"] = "Group '{0}' granted permission '{1}'",
                ["GroupPermissionRevoked"] = "Group '{0}' revoked permission '{1}'",
                ["GroupPermissions"] = "Group '{0}' permissions",
                ["GroupPlayers"] = "Group '{0}' players",
                ["Groups"] = "Groups",
                ["NoGroupPermissions"] = "No permissions currently granted",
                ["NoPermissionGroups"] = "No groups with this permission",
                ["NoPermissionPlayers"] = "No players with this permission",
                ["NoPluginsFound"] = "No plugins are currently available",
                ["NoPlayerGroups"] = "Player is not assigned to any groups",
                ["NoPlayerPermissions"] = "No permissions currently granted",
                ["NoPlayersInGroup"] = "No players currently in group",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["ParentGroupPermissions"] = "Parent group '{0}' permissions",
                ["PermissionGroups"] = "Permission '{0}' Groups",
                ["PermissionPlayers"] = "Permission '{0}' Players",
                ["PermissionNotFound"] = "Permission '{0}' doesn't exist",
                ["Permissions"] = "Permissions",
                ["PermissionsNotLoaded"] = "Unable to load permission files! Permissions will not work until resolved.\n => {0}",
                ["PlayerLanguage"] = "Player language set to {0}",
                ["PluginNotLoaded"] = "Plugin '{0}' not loaded.",
                ["PluginReloaded"] = "Reloaded plugin {0} v{1} by {2}",
                ["PluginUnloaded"] = "Unloaded plugin {0} v{1} by {2}",
                ["ServerLanguage"] = "Server language set to {0}",
                ["Unknown"] = "Unknown",
                ["UnknownCommand"] = "Unknown command: {0}",
                ["PlayerAddedToGroup"] = "Player '{0}' added to group: {1}",
                ["PlayerAlreadyHasPermission"] = "Player '{0}' already has permission '{1}'",
                ["PlayerDoesNotHavePermission"] = "Player '{0}' does not have permission '{1}'",
                ["PlayerNotFound"] = "Player '{0}' not found",
                ["PlayerGroups"] = "Player '{0}' groups",
                ["PlayerPermissions"] = "Player '{0}' permissions",
                ["PlayerPermissionGranted"] = "Player '{0}' granted permission '{1}'",
                ["PlayerPermissionRevoked"] = "Player '{0}' revoked permission '{1}'",
                ["PlayerRemovedFromGroup"] = "Player '{0}' removed from group '{1}'",
                ["PlayersFound"] = "Multiple players were found, please specify: {0}",
                ["Version"] = "Server is running [#ffb658]Oxide {0}[/#] and [#ee715c]{1} {2} ({3})[/#]",
                ["YouAreNotAdmin"] = "You are not an admin"
            }

            #endregion English
        };
    }
}
