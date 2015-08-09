using System.Security.Permissions;
using MySql.Data.MySqlClient;
using Oxide.Core.Plugins;

namespace Oxide.Ext.MySql
{
    [ReflectionPermission(SecurityAction.Deny, Flags = ReflectionPermissionFlag.AllFlags)]
    public sealed class Connection
    {
        internal string ConnectionString { get; set; }
        internal bool ConnectionPersistent { get; set; }
        internal MySqlConnection Con { get; set; }
        internal Plugin Plugin { get; set; }

        public Connection(string connection, bool persistent)
        {
            ConnectionString = connection;
            ConnectionPersistent = persistent;
        }
    }
}
