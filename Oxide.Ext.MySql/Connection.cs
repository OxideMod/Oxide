using System.Security.Permissions;
using MySql.Data.MySqlClient;

namespace Oxide.Ext.MySql
{
    [ReflectionPermission(SecurityAction.Deny, Flags = ReflectionPermissionFlag.AllFlags)]
    public sealed class Connection
    {
        internal MySqlConnection Con;
        public Connection(string connection)
        {
            Con = new MySqlConnection(connection);
        }
    }
}
