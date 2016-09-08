﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

using MySql.Data.MySqlClient;

using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Ext.MySql.Libraries
{
    public class MySql : Library, IDatabaseProvider
    {
        public override bool IsGlobal => false;

        private readonly Queue<MySqlQuery> _queue = new Queue<MySqlQuery>();
        private readonly object _syncroot = new object();
        private readonly AutoResetEvent _workevent = new AutoResetEvent(false);
        private readonly HashSet<Connection> _runningConnections = new HashSet<Connection>();
        private bool _running = true;
        private readonly Dictionary<string, Dictionary<string, Connection>> _connections = new Dictionary<string, Dictionary<string, Connection>>();
        private readonly Thread _worker;
        private readonly Dictionary<Plugin, Event.Callback<Plugin, PluginManager>> _pluginRemovedFromManager;

        /// <summary>
        /// Represents a single MySqlQuery instance
        /// </summary>
        public class MySqlQuery
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

            private MySqlCommand _cmd;
            private MySqlConnection _connection;
            private IAsyncResult _result;

            private void Cleanup()
            {
                if (_cmd != null)
                {
                    _cmd.Dispose();
                    _cmd = null;
                }
                _connection = null;
            }

            public bool Handle()
            {
                List<Dictionary<string, object>> list = null;
                var nonQueryResult = 0;
                var lastInsertRowId = 0L;
                try
                {
                    if (Connection == null) throw new Exception("Connection is null");
                    //if (_result == null)
                    //{
                        _connection = (MySqlConnection) Connection.Con;
                        if (_connection.State == ConnectionState.Closed)
                            _connection.Open();
                        _cmd = _connection.CreateCommand();
                        _cmd.CommandTimeout = 120;
                        _cmd.CommandText = Sql.SQL;
                        Sql.AddParams(_cmd, Sql.Arguments, "@");
                        _result = NonQuery ? _cmd.BeginExecuteNonQuery() : _cmd.BeginExecuteReader();
                    //}
                    _result.AsyncWaitHandle.WaitOne();
                    //if (!_result.IsCompleted) return false;
                    if (NonQuery)
                        nonQueryResult = _cmd.EndExecuteNonQuery(_result);
                    else
                    {
                        using (var reader = _cmd.EndExecuteReader(_result))
                        {
                            list = new List<Dictionary<string, object>>();
                            while (reader.Read())
                            {
                                if (Connection.ConnectionPersistent && (Connection.Con.State == ConnectionState.Closed || Connection.Con.State == ConnectionState.Broken)) {
									break;
								}
                                var dict = new Dictionary<string, object>();
                                for (var i = 0; i < reader.FieldCount; i++)
                                {
                                    dict.Add(reader.GetName(i), reader.GetValue(i));
                                }
                                list.Add(dict);
                            }
                        }
                    }
                    lastInsertRowId = _cmd.LastInsertedId;
                    Cleanup();
                }
                catch (Exception ex)
                {
                    var message = "MySql handle raised an exception";
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
                        var message = "MySql command callback raised an exception";
                        if (Connection?.Plugin != null) message += $" in '{Connection.Plugin.Name} v{Connection.Plugin.Version}' plugin";
                        Interface.Oxide.LogException(message, ex);
                    }
                    Connection?.Plugin?.TrackEnd();
                });
                return true;
            }
        }

        public MySql()
        {
            _pluginRemovedFromManager = new Dictionary<Plugin, Event.Callback<Plugin, PluginManager>>();
            _worker = new Thread(Worker);
            _worker.Start();
        }

        /// <summary>
        /// The worker thread method
        /// </summary>
        private void Worker()
        {
            while (_running || _queue.Count > 0)
            {
                MySqlQuery query = null;
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
                    //if (!query.Handle()) continue;
                    if (query.Connection != null) _runningConnections.Add(query.Connection);
                    //lock (_syncroot) _queue.Dequeue();
                }
                else if (_running)
                    _workevent.WaitOne();
            }
        }

        [LibraryFunction("OpenDb")]
        public Connection OpenDb(string host, int port, string database, string user, string password, Plugin plugin, bool persistent = false)
        {
            return OpenDb($"Server={host};Port={port};Database={database};User={user};Password={password};Pooling=false;CharSet=utf8;default command timeout=120;", plugin, persistent);
        }

        public Connection OpenDb(string conStr, Plugin plugin, bool persistent = false)
        {
            Dictionary<string, Connection> connections;
            if (!_connections.TryGetValue(plugin?.Name ?? "null", out connections))
                _connections[plugin?.Name ?? "null"] = connections = new Dictionary<string, Connection>();
            Connection connection;
            if (connections.TryGetValue(conStr, out connection))
            {
                Interface.Oxide.LogWarning("Already open connection ({0}), using existing instead...", connection.Con?.ConnectionString);
            }
            else
            {
                connection = new Connection(conStr, persistent)
                {
                    Plugin = plugin,
                    Con = new MySqlConnection(conStr)
                };
                connections[conStr] = connection;
            }
            if (plugin != null && !_pluginRemovedFromManager.ContainsKey(plugin))
                _pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(OnRemovedFromManager);
            return connection;
        }

        private void OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            Dictionary<string, Connection> connections;
            if (_connections.TryGetValue(sender.Name, out connections))
            {
                foreach (var connection in connections)
                {
                    if (connection.Value.Plugin != sender) continue;
                    if (connection.Value.Con?.State != ConnectionState.Closed)
                        Interface.Oxide.LogWarning("Unclosed mysql connection ({0}), by plugin '{1}', closing...", connection.Value.Con?.ConnectionString, connection.Value.Plugin?.Name ?? "null");
                    connection.Value.Con?.Close();
                    connection.Value.Plugin = null;
                }
                _connections.Remove(sender.Name);
            }
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
            Dictionary<string, Connection> connections;
            if (_connections.TryGetValue(db.Plugin?.Name ?? "null", out connections))
            {
                connections.Remove(db.ConnectionString);
                if (connections.Count == 0)
                {
                    _connections.Remove(db.Plugin?.Name ?? "null");
                    if (db.Plugin != null)
                    {
                        Event.Callback<Plugin, PluginManager> event_callback;
                        if (_pluginRemovedFromManager.TryGetValue(db.Plugin, out event_callback))
                        {
                            event_callback.Remove();
                            _pluginRemovedFromManager.Remove(db.Plugin);
                        }
                    }
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
            var query = new MySqlQuery
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
            var query = new MySqlQuery
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
