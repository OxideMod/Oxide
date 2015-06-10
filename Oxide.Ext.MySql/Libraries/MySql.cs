using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MySql.Data.MySqlClient;

using Oxide.Core;
using Oxide.Core.Libraries;

namespace Oxide.Ext.MySql.Libraries
{
    public class MySql : Library
    {
        public override bool IsGlobal => false;

        private readonly Queue<MySqlQuery> _queue = new Queue<MySqlQuery>();
        private readonly object _syncroot = new object();
        private readonly AutoResetEvent _workevent = new AutoResetEvent(false);
        private bool _running = true;

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
            public string Connection { get; internal set; }

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
                if (_connection == null) return;
                _connection.Close();
                _connection = null;
            }

            public bool Handle()
            {
                List<Dictionary<string, object>> list = null;
                var nonQueryResult = 0;
                try
                {
                    if (_result == null)
                    {
                        _connection = new MySqlConnection(Connection);
                        _connection.Open();
                        _cmd = _connection.CreateCommand();
                        _cmd.CommandText = Sql.SQL;
                        Sql.AddParams(_cmd, Sql.Arguments, "@");
                        _result = NonQuery ? _cmd.BeginExecuteNonQuery() : _cmd.BeginExecuteReader();
                    }
                    if (!_result.IsCompleted) return false;
                    if (NonQuery)
                        nonQueryResult = _cmd.EndExecuteNonQuery(_result);
                    else
                    {
                        using (var reader = _cmd.EndExecuteReader(_result))
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
                    Cleanup();
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException("Exception raised in mysql handle", ex);
                    Cleanup();
                }
                Interface.Oxide.NextTick(() =>
                {
                    try
                    {
                        if (!NonQuery)
                            Callback(list);
                        else
                            CallbackNonQuery?.Invoke(nonQueryResult);
                    }
                    catch (Exception ex)
                    {
                        Interface.Oxide.LogException("Exception raised in mysql command callback", ex);
                    }
                });
                return true;
            }
        }

        public MySql()
        {
            new Thread(Worker) { IsBackground = true }.Start();
        }

        /// <summary>
        /// The worker thread method
        /// </summary>
        private void Worker()
        {
            while (_running)
            {
                if (_queue.Count < 1)
                {
                    _workevent.Reset();
                    _workevent.WaitOne();
                }
                MySqlQuery query;
                lock (_syncroot) query = _queue.Dequeue();
                if (query.Handle()) continue;
                lock (_syncroot) _queue.Enqueue(query);
            }
        }

        [LibraryFunction("OpenDb")]
        public Connection OpenDb(string host, int port, string database, string user, string password)
        {
            return new Connection(string.Format("Server={0};Port={1};Database={2};User={3};Password={4};Pooling=false;CharSet=utf8;", host, port, database, user, password));
        }

        [LibraryFunction("NewSql")]
        public Sql NewSql()
        {
            return Sql.Builder;
        }

        [LibraryFunction("QueryAsync")]
        public void QueryAsync(Sql sql, Connection db, Action<List<Dictionary<string, object>>> callback)
        {
            var query = new MySqlQuery
            {
                Sql = sql,
                Connection = db.ConnectionString,
                Callback = callback
            };
            lock (_syncroot) _queue.Enqueue(query);
            _workevent.Set();
        }

        [LibraryFunction("NonQueryAsync")]
        public void NonQueryAsync(Sql sql, Connection db, Action<int> callback = null)
        {
            var query = new MySqlQuery
            {
                Sql = sql,
                Connection = db.ConnectionString,
                CallbackNonQuery = callback,
                NonQuery = true
            };
            lock (_syncroot) _queue.Enqueue(query);
            _workevent.Set();
        }

        [LibraryFunction("InsertAsync")]
        public void InsertAsync(Sql sql, Connection db, Action<int> callback = null)
        {
            NonQueryAsync(sql, db, callback);
        }

        [LibraryFunction("UpdateAsync")]
        public void UpdateAsync(Sql sql, Connection db, Action<int> callback = null)
        {
            NonQueryAsync(sql, db, callback);
        }

        [LibraryFunction("DeleteAsync")]
        public void DeleteAsync(Sql sql, Connection db, Action<int> callback = null)
        {
            NonQueryAsync(sql, db, callback);
        }

        [LibraryFunction("Query")]
        public IEnumerable<Dictionary<string, object>> Query(Sql sql, Connection db)
        {
            MySqlConnection con = null;
            try
            {
                con = new MySqlConnection(db.ConnectionString);
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        var list = new List<Dictionary<string, object>>();
                        while (dataReader.Read())
                        {
                            var dict = new Dictionary<string, object>();
                            for (var i = 0; i < dataReader.FieldCount; i++)
                            {
                                dict.Add(dataReader.GetName(i), dataReader.GetValue(i));
                            }
                            list.Add(dict);
                        }
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("Query failed", ex);
                return null;
            }
            finally
            {
                con?.Close();
            }
        }

        [LibraryFunction("ExecuteNonQuery")]
        public int ExecuteNonQuery(Sql sql, Connection db)
        {
            MySqlConnection con = null;
            try
            {
                con = new MySqlConnection(db.ConnectionString);
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("ExecuteNonQuery failed", ex);
                return 0;
            }
            finally
            {
                con?.Close();
            }
        }

        [LibraryFunction("Insert")]
        public int Insert(Sql sql, Connection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        [LibraryFunction("Update")]
        public int Update(Sql sql, Connection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        [LibraryFunction("Delete")]
        public int Delete(Sql sql, Connection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        public T ExecuteScalar<T>(Sql sql, Connection db)
        {
            MySqlConnection con = null;
            try
            {
                con = new MySqlConnection(db.ConnectionString);
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    var val = cmd.ExecuteScalar();
                    return (T) Convert.ChangeType(val, typeof (T));
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("ExecuteScalar failed", ex);
                return default(T);
            }
            finally
            {
                con?.Close();
            }
        }

        [LibraryFunction("ExecuteScalar")]
        public string ExecuteScalar(Sql sql, Connection db)
        {
            return ExecuteScalar<string>(sql, db);
        }

        [LibraryFunction("First")]
        public Dictionary<string, object> First(Sql sql, Connection db)
        {
            return Query(sql, db).First();
        }

        [LibraryFunction("FirstOrDefault")]
        public Dictionary<string, object> FirstOrDefault(Sql sql, Connection db)
        {
            return Query(sql, db).FirstOrDefault();
        }

        internal void Shutdown()
        {
            _running = false;
        }
    }
}
