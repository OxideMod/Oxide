using System.Data.Common;
using System.Security.Permissions;
using Oxide.Core.Plugins;

namespace Oxide.Core.Database
{
    [ReflectionPermission(SecurityAction.Deny, Flags = ReflectionPermissionFlag.AllFlags)]
    public sealed class Connection
    {
        public string ConnectionString { get; set; }
        public bool ConnectionPersistent { get; set; }
        public DbConnection Con { get; set; }
        public Plugin Plugin { get; set; }
        public long LastInsertRowId { get; set; }

        public Connection(string connection, bool persistent)
        {
            ConnectionString = connection;
            ConnectionPersistent = persistent;
        }
    }
}
