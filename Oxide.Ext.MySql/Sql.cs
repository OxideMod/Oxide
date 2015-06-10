using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide.Ext.MySql
{
    // Simple helper class for building SQL statements
    public class Sql
    {
        private static readonly Regex Filter = new Regex(@"LOAD\s*DATA|INTO\s*(OUTFILE|DUMPFILE)|LOAD_FILE", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RxParams = new Regex(@"(?<!@)@\w+", RegexOptions.Compiled);
        private readonly object[] _args;
        private readonly string _sql;
        private object[] _argsFinal;
        private Sql _rhs;
        private string _sqlFinal;

        public Sql()
        {
        }

        public Sql(string sql, params object[] args)
        {
            _sql = sql;
            _args = args;
        }

        public static Sql Builder => new Sql();

        public string SQL
        {
            get
            {
                Build();
                return _sqlFinal;
            }
        }

        public object[] Arguments
        {
            get
            {
                Build();
                return _argsFinal;
            }
        }

        private void Build()
        {
            // Already built?
            if (_sqlFinal != null)
                return;

            // Build it
            var sb = new StringBuilder();
            var args = new List<object>();
            Build(sb, args, null);
            var tmpFinal = sb.ToString();
            if (Filter.IsMatch(tmpFinal))
                throw new Exception("Commands LOAD DATA, LOAD_FILE, OUTFILE, DUMPFILE not allowed.");
            _sqlFinal = tmpFinal;
            _argsFinal = args.ToArray();
        }

        public Sql Append(Sql sql)
        {
            if (_rhs != null)
                _rhs.Append(sql);
            else
                _rhs = sql;

            return this;
        }

        public Sql Append(string sql, params object[] args)
        {
            return Append(new Sql(sql, args));
        }

        private static bool Is(Sql sql, string sqltype)
        {
            return sql?._sql != null && sql._sql.StartsWith(sqltype, StringComparison.InvariantCultureIgnoreCase);
        }

        private void Build(StringBuilder sb, List<object> args, Sql lhs)
        {
            if (!String.IsNullOrEmpty(_sql))
            {
                // Add SQL to the string
                if (sb.Length > 0)
                {
                    sb.Append("\n");
                }

                var sql = ProcessParams(_sql, _args, args);

                if (Is(lhs, "WHERE ") && Is(this, "WHERE "))
                    sql = "AND " + sql.Substring(6);
                if (Is(lhs, "ORDER BY ") && Is(this, "ORDER BY "))
                    sql = ", " + sql.Substring(9);

                sb.Append(sql);
            }

            // Now do rhs
            _rhs?.Build(sb, args, this);
        }

        public Sql Where(string sql, params object[] args)
        {
            return Append(new Sql("WHERE (" + sql + ")", args));
        }

        public Sql OrderBy(params object[] columns)
        {
            return Append(new Sql("ORDER BY " + String.Join(", ", (from x in columns select x.ToString()).ToArray())));
        }

        public Sql Select(params object[] columns)
        {
            return Append(new Sql("SELECT " + String.Join(", ", (from x in columns select x.ToString()).ToArray())));
        }

        public Sql From(params object[] tables)
        {
            return Append(new Sql("FROM " + String.Join(", ", (from x in tables select x.ToString()).ToArray())));
        }

        public Sql GroupBy(params object[] columns)
        {
            return Append(new Sql("GROUP BY " + String.Join(", ", (from x in columns select x.ToString()).ToArray())));
        }

        private SqlJoinClause Join(string joinType, string table)
        {
            return new SqlJoinClause(Append(new Sql(joinType + table)));
        }

        public SqlJoinClause InnerJoin(string table)
        {
            return Join("INNER JOIN ", table);
        }

        public SqlJoinClause LeftJoin(string table)
        {
            return Join("LEFT JOIN ", table);
        }

        public static string ProcessParams(string sql, object[] argsSrc, List<object> argsDest)
        {
            return RxParams.Replace(sql, m =>
            {
                var param = m.Value.Substring(1);

                object argVal;

                int paramIndex;
                if (int.TryParse(param, out paramIndex))
                {
                    if (paramIndex < 0 || paramIndex >= argsSrc.Length)
                        throw new ArgumentOutOfRangeException(string.Format("Parameter '@{0}' specified but only {1} parameters supplied (in `{2}`)", paramIndex, argsSrc.Length, sql));
                    argVal = argsSrc[paramIndex];
                }
                else
                {
                    var found = false;
                    argVal = null;
                    foreach (var o in argsSrc)
                    {
                        var pi = o.GetType().GetProperty(param);
                        if (pi == null) continue;
                        argVal = pi.GetValue(o, null);
                        found = true;
                        break;
                    }

                    if (!found)
                        throw new ArgumentException(string.Format("Parameter '@{0}' specified but none of the passed arguments have a property with this name (in '{1}')", param, sql));
                }

                if ((argVal as IEnumerable) != null && (argVal as string) == null && (argVal as byte[]) == null)
                {
                    var sb = new StringBuilder();
                    foreach (var i in argVal as IEnumerable)
                    {
                        sb.Append((sb.Length == 0 ? "@" : ",@") + argsDest.Count.ToString());
                        argsDest.Add(i);
                    }
                    return sb.ToString();
                }
                argsDest.Add(argVal);
                return "@" + (argsDest.Count - 1).ToString();
            }
                );
        }

        public static void AddParams(IDbCommand cmd, object[] items, string parameterPrefix)
        {
            foreach (var item in items)
            {
                AddParam(cmd, item, "@");
            }
        }

        public static void AddParam(IDbCommand cmd, object item, string parameterPrefix)
        {
            var idbParam = item as IDbDataParameter;
            if (idbParam != null)
            {
                idbParam.ParameterName = string.Format("{0}{1}", parameterPrefix, cmd.Parameters.Count);
                cmd.Parameters.Add(idbParam);
                return;
            }

            var p = cmd.CreateParameter();
            p.ParameterName = string.Format("{0}{1}", parameterPrefix, cmd.Parameters.Count);
            if (item == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                var t = item.GetType();
                if (t.IsEnum)
                {
                    p.Value = (int) item;
                }
                else if (t == typeof (Guid))
                {
                    p.Value = item.ToString();
                    p.DbType = DbType.String;
                    p.Size = 40;
                }
                else if (t == typeof (string))
                {
                    p.Size = Math.Max(((string) item).Length + 1, 4000);
                    p.Value = item;
                }
                else if (t == typeof (bool))
                {
                    p.Value = ((bool) item) ? 1 : 0;
                }
                else
                {
                    p.Value = item;
                }
            }

            cmd.Parameters.Add(p);
        }

        public class SqlJoinClause
        {
            private readonly Sql _sql;

            public SqlJoinClause(Sql sql)
            {
                _sql = sql;
            }

            public Sql On(string onClause, params object[] args)
            {
                return _sql.Append("ON " + onClause, args);
            }
        }
    }
}
