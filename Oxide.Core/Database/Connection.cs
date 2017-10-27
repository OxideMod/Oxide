using System.Data.Common;
#if NET35
using System.Security.Permissions;
#endif
using Oxide.Core.Plugins;

namespace Oxide.Core.Database
{
#if NET35
    [ReflectionPermission(SecurityAction.Deny, Flags = ReflectionPermissionFlag.AllFlags)]
#endif

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
