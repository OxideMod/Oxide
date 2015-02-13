using System;
using System.Collections.Generic;
using System.Linq;

using Oxide.Core.Libraries;

namespace Oxide.Ext.MySql.Libraries
{
    public class MySql : Library
    {
        public override bool IsGlobal { get { return false; } }

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

        [LibraryFunction("Query")]
        public IEnumerable<Dictionary<string, object>> Query(Sql sql, Connection db)
        {
            try
            {
                db.Con.Open();
                using (var cmd = db.Con.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var dict = new Dictionary<string, object>();
                            for (var i = 0; i < dataReader.FieldCount; i++)
                            {
                                dict.Add(dataReader.GetName(i), dataReader.GetValue(i));
                            }
                            yield return dict;
                        }
                    }
                }
            }
            finally
            {
                db.Con.Close();
            }
        }

        [LibraryFunction("ExecuteNonQuery")]
        public int ExecuteNonQuery(Sql sql, Connection db)
        {
            try
            {
                db.Con.Open();
                using (var cmd = db.Con.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                db.Con.Close();
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
            try
            {
                db.Con.Open();
                using (var cmd = db.Con.CreateCommand())
                {
                    cmd.CommandText = sql.SQL;
                    Sql.AddParams(cmd, sql.Arguments, "@");
                    var val = cmd.ExecuteScalar();
                    return (T) Convert.ChangeType(val, typeof (T));
                }
            }
            finally
            {
                db.Con.Close();
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
    }
}
