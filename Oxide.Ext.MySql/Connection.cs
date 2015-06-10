using System.Security.Permissions;

namespace Oxide.Ext.MySql
{
    [ReflectionPermission(SecurityAction.Deny, Flags = ReflectionPermissionFlag.AllFlags)]
    public sealed class Connection
    {
        internal string ConnectionString;
        public Connection(string connection)
        {
            ConnectionString = connection;
        }
    }
}
