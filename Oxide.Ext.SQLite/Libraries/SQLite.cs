using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Libraries;

namespace Oxide.Ext.SQLite.Libraries
{
    public class SQLite : Library
    {
        private readonly string _dataDirectory;

        public override bool IsGlobal { get { return false; } }

        public SQLite()
        {
            _dataDirectory = Interface.GetMod().DataDirectory;
        }

        [LibraryFunction("OpenDb")]
        public SQLiteConnection OpenDb(string file)
        {
            var filename = Path.Combine(_dataDirectory, file);
            if (!filename.StartsWith(_dataDirectory, StringComparison.Ordinal))
                throw new Exception("Only access to oxide directory!");
            return new SQLiteConnection(string.Format("Data Source={0};Version=3;", filename));
        }

        [LibraryFunction("NewSql")]
        public Sql NewSql()
        {
            return Sql.Builder;
        }

        [LibraryFunction("Query")]
        public IEnumerable<Dictionary<string, object>> Query(Sql sql, SQLiteConnection db)
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
            finally
            {
                db.Close();
            }
        }

        [LibraryFunction("ExecuteNonQuery")]
        public int ExecuteNonQuery(Sql sql, SQLiteConnection db)
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
        public int Insert(Sql sql, SQLiteConnection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        [LibraryFunction("Update")]
        public int Update(Sql sql, SQLiteConnection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        [LibraryFunction("Delete")]
        public int Delete(Sql sql, SQLiteConnection db)
        {
            return ExecuteNonQuery(sql, db);
        }

        public T ExecuteScalar<T>(Sql sql, SQLiteConnection db)
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
        public string ExecuteScalar(Sql sql, SQLiteConnection db)
        {
            return ExecuteScalar<string>(sql, db);
        }

        [LibraryFunction("First")]
        public Dictionary<string, object> First(Sql sql, SQLiteConnection db)
        {
            return Query(sql, db).First();
        }

        [LibraryFunction("FirstOrDefault")]
        public Dictionary<string, object> FirstOrDefault(Sql sql, SQLiteConnection db)
        {
            return Query(sql, db).FirstOrDefault();
        }
    }
}
