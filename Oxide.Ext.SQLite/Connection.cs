using System.Data.SQLite;
using System.Security.Permissions;
using Oxide.Core.Plugins;

namespace Oxide.Ext.SQLite
{
    [ReflectionPermission(SecurityAction.Deny, Flags = ReflectionPermissionFlag.AllFlags)]
    public sealed class Connection
    {
        internal string ConnectionString { get; set; }
        internal bool ConnectionPersistent { get; set; }
        internal SQLiteConnection Con { get; set; }
        internal Plugin Plugin { get; set; }

        public Connection(string connection, bool persistent)
        {
            ConnectionString = connection;
            ConnectionPersistent = persistent;
        }
    }
}
