using System;
using System.Collections.Generic;
using System.Linq;

using MySql.Data.MySqlClient;

using Oxide.Core.Libraries;

namespace Oxide.Ext.MySql.Libraries
{
    public class MySql : Library
    {
        public override bool IsGlobal { get { return false; } }

        [LibraryFunction("OpenDb")]
        public MySqlConnection OpenDb(string host, int port, string database, string user, string password)
        {
            return new MySqlConnection(string.Format("Server={0};Port={1};Database={2};User={3};Password={4};Pooling=false;CharSet=utf8;", host, port, database, user, password));
        }

        [LibraryFunction("NewSql")]
        public Sql NewSql()
        {
            return Sql.Builder;
        }

        [LibraryFunction("Query")]
        public IEnumerable<Dictionary<string, object>> Query(Sql sql, MySqlConnection db)
        {
            try
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        //var list = new List<Dictionary<string, object>>();
                        while (dataReader.Read())
                        {
                            var dict = new Dictionary<string, object>();
                            for (var i = 0; i < dataReader.FieldCount; i++)
                            {
                                dict.Add(dataReader.GetName(i), dataReader.GetValue(i));
                            }
                            yield return dict;
                            //list.Add(dict);
                        }
                        //return list;
                    }
                }
            }
            finally
            {
                db.Close();
            }
        }

        [LibraryFunction("ExecuteNonQuery")]
        public int ExecuteNonQuery(Sql sql, MySqlConnection db)
        {
            try
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                db.Close();
            }
        }

        [LibraryFunction("Insert")]
        public int Insert(Sql sql, MySqlConnection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        [LibraryFunction("Update")]
        public int Update(Sql sql, MySqlConnection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        [LibraryFunction("Delete")]
        public int Delete(Sql sql, MySqlConnection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        public T ExecuteScalar<T>(Sql sql, MySqlConnection db)
        {
            try
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    var val = cmd.ExecuteScalar();
                    return (T) Convert.ChangeType(val, typeof (T));
                }
            }
            finally
            {
                db.Close();
            }
        }

        [LibraryFunction("ExecuteScalar")]
        public string ExecuteScalar(Sql sql, MySqlConnection db)
        {
            return ExecuteScalar<string>(sql, db);
        }

        [LibraryFunction("First")]
        public Dictionary<string, object> First(Sql sql, MySqlConnection db)
        {
            return Query(sql, db).First();
        }

        [LibraryFunction("FirstOrDefault")]
        public Dictionary<string, object> FirstOrDefault(Sql sql, MySqlConnection db)
        {
            return Query(sql, db).FirstOrDefault();
        }
    }
}
