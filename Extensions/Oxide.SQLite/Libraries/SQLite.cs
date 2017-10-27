using Oxide.Core.Database;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;

namespace Oxide.Core.SQLite.Libraries
{
    public class SQLite : Library, IDatabaseProvider
    {
        private readonly string _dataDirectory;

        public override bool IsGlobal => false;

        private readonly Queue<SQLiteQuery> _queue = new Queue<SQLiteQuery>();
        private readonly object _syncroot = new object();
        private readonly AutoResetEvent _workevent = new AutoResetEvent(false);
        private readonly HashSet<Connection> _runningConnections = new HashSet<Connection>();
        private bool _running = true;
        private readonly Dictionary<string, Connection> _connections = new Dictionary<string, Connection>();
        private readonly Thread _worker;
        private readonly Dictionary<Plugin, Event.Callback<Plugin, PluginManager>> _pluginRemovedFromManager;

        /// <summary>
        /// Represents a single MySqlQuery instance
        /// </summary>
        public class SQLiteQuery
        {
            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action<List<Dictionary<string, object>>> Callback { get; internal set; }

            /// <summary>
            /// Gets the callback delegate
            /// </summary>
            public Action<int> CallbackNonQuery { get; internal set; }

            /// <summary>
            /// Gets the sql
            /// </summary>
            public Sql Sql { get; internal set; }

            /// <summary>
            /// Gets the connection
            /// </summary>
            public Connection Connection { get; internal set; }

            /// <summary>
            /// Gets the non query
            /// </summary>
            public bool NonQuery { get; internal set; }

            private SQLiteCommand _cmd;
            private SQLiteConnection _connection;

            private void Cleanup()
            {
                if (_cmd != null)
                {
                    _cmd.Dispose();
                    _cmd = null;
                }
                _connection = null;
            }

            public void Handle()
            {
                List<Dictionary<string, object>> list = null;
                var nonQueryResult = 0;
                var lastInsertRowId = 0L;
                try
                {
                    if (Connection == null) throw new Exception("Connection is null");
                    _connection = (SQLiteConnection)Connection.Con;
                    if (_connection.State == ConnectionState.Closed)
                        _connection.Open();
                    _cmd = _connection.CreateCommand();
                    _cmd.CommandText = Sql.SQL;
                    Sql.AddParams(_cmd, Sql.Arguments, "@");
                    if (NonQuery)
                        nonQueryResult = _cmd.ExecuteNonQuery();
                    else
                    {
                        using (var reader = _cmd.ExecuteReader())
                        {
                            list = new List<Dictionary<string, object>>();
                            while (reader.Read())
                            {
                                var dict = new Dictionary<string, object>();
                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    dict.Add(reader.GetName(i), reader.GetValue(i));
                                }
                                list.Add(dict);
                            }
                        }
                    }
                    lastInsertRowId = _connection.LastInsertRowId;
                    Cleanup();
                }
                catch (Exception ex)
                {
                    var message = "Sqlite handle raised an exception";
                    if (Connection?.Plugin != null) message += $" in '{Connection.Plugin.Name} v{Connection.Plugin.Version}' plugin";
                    Interface.Oxide.LogException(message, ex);
                    Cleanup();
                }
                Interface.Oxide.NextTick(() =>
                {
                    Connection?.Plugin?.TrackStart();
                    try
                    {
                        if (Connection != null) Connection.LastInsertRowId = lastInsertRowId;
                        if (!NonQuery)
                            Callback(list);
                        else
                            CallbackNonQuery?.Invoke(nonQueryResult);
                    }
                    catch (Exception ex)
                    {
                        var message = "Sqlite command callback raised an exception";
                        if (Connection?.Plugin != null) message += $" in '{Connection.Plugin.Name} v{Connection.Plugin.Version}' plugin";
                        Interface.Oxide.LogException(message, ex);
                    }
                    Connection?.Plugin?.TrackEnd();
                });
            }
        }

        /// <summary>
        /// The worker thread method
        /// </summary>
        private void Worker()
        {
            while (_running || _queue.Count > 0)
            {
                SQLiteQuery query = null;
                lock (_syncroot)
                {
                    if (_queue.Count > 0)
                        query = _queue.Dequeue();
                    else
                    {
                        foreach (var connection in _runningConnections)
                            if (connection != null && !connection.ConnectionPersistent) CloseDb(connection);
                        _runningConnections.Clear();
                    }
                }
                if (query != null)
                {
                    query.Handle();
                    if (query.Connection != null) _runningConnections.Add(query.Connection);
                }
                else if (_running)
                    _workevent.WaitOne();
            }
        }

        public SQLite()
        {
            _dataDirectory = Interface.Oxide.DataDirectory;
            _pluginRemovedFromManager = new Dictionary<Plugin, Event.Callback<Plugin, PluginManager>>();
            _worker = new Thread(Worker);
            _worker.Start();
        }

        [LibraryFunction("OpenDb")]
        public Connection OpenDb(string file, Plugin plugin, bool persistent = false)
        {
            if (string.IsNullOrEmpty(file)) return null;
            var filename = Path.Combine(_dataDirectory, file);
            if (!filename.StartsWith(_dataDirectory, StringComparison.Ordinal))
                throw new Exception("Only access to oxide directory!");
            var conStr = $"Data Source={filename};Version=3;";
            Connection connection;
            if (_connections.TryGetValue(conStr, out connection))
            {
                if (plugin != connection.Plugin)
                {
                    Interface.Oxide.LogWarning("Already open connection ({0}), by plugin '{1}'...", conStr, connection.Plugin);
                    return null;
                }
                Interface.Oxide.LogWarning("Already open connection ({0}), using existing instead...", conStr);
            }
            else
            {
                connection = new Connection(conStr, persistent)
                {
                    Plugin = plugin,
                    Con = new SQLiteConnection(conStr)
                };
                _connections[conStr] = connection;
            }
            if (plugin != null && !_pluginRemovedFromManager.ContainsKey(plugin))
                _pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(OnRemovedFromManager);
            return connection;
        }

        private void OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            var toRemove = new List<string>();
            foreach (var connection in _connections)
            {
                if (connection.Value.Plugin != sender) continue;
                if (connection.Value.Con?.State != ConnectionState.Closed)
                    Interface.Oxide.LogWarning("Unclosed sqlite connection ({0}), by plugin '{1}', closing...", connection.Value.ConnectionString, connection.Value.Plugin?.Name ?? "null");
                connection.Value.Con?.Close();
                connection.Value.Plugin = null;
                toRemove.Add(connection.Key);
            }
            foreach (var conStr in toRemove)
                _connections.Remove(conStr);
            Event.Callback<Plugin, PluginManager> event_callback;
            if (_pluginRemovedFromManager.TryGetValue(sender, out event_callback))
            {
                event_callback.Remove();
                _pluginRemovedFromManager.Remove(sender);
            }
        }

        [LibraryFunction("CloseDb")]
        public void CloseDb(Connection db)
        {
            if (db == null) return;
            _connections.Remove(db.ConnectionString);
            if (db.Plugin != null && _connections.Values.All(c => c.Plugin != db.Plugin))
            {
                Event.Callback<Plugin, PluginManager> event_callback;
                if (_pluginRemovedFromManager.TryGetValue(db.Plugin, out event_callback))
                {
                    event_callback.Remove();
                    _pluginRemovedFromManager.Remove(db.Plugin);
                }
            }
            db.Con?.Close();
            db.Plugin = null;
        }

        [LibraryFunction("NewSql")]
        public Sql NewSql()
        {
            return Sql.Builder;
        }

        [LibraryFunction("Query")]
        public void Query(Sql sql, Connection db, Action<List<Dictionary<string, object>>> callback)
        {
            var query = new SQLiteQuery
            {
                Sql = sql,
                Connection = db,
                Callback = callback
            };
            lock (_syncroot) _queue.Enqueue(query);
            _workevent.Set();
        }

        [LibraryFunction("ExecuteNonQuery")]
        public void ExecuteNonQuery(Sql sql, Connection db, Action<int> callback = null)
        {
            var query = new SQLiteQuery
            {
                Sql = sql,
                Connection = db,
                CallbackNonQuery = callback,
                NonQuery = true
            };
            lock (_syncroot) _queue.Enqueue(query);
            _workevent.Set();
        }

        [LibraryFunction("Insert")]
        public void Insert(Sql sql, Connection db, Action<int> callback = null)
        {
            ExecuteNonQuery(sql, db, callback);
        }

        [LibraryFunction("Update")]
        public void Update(Sql sql, Connection db, Action<int> callback = null)
        {
            ExecuteNonQuery(sql, db, callback);
        }

        [LibraryFunction("Delete")]
        public void Delete(Sql sql, Connection db, Action<int> callback = null)
        {
            ExecuteNonQuery(sql, db, callback);
        }

        public override void Shutdown()
        {
            _running = false;
            _workevent.Set();
            _worker.Join();
        }
    }
}
