using NLua;
using Oxide.Core.Libraries;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Core.Lua.Libraries
{
    /// <summary>
    /// A utility library for Lua specific functions
    /// </summary>
    public class LuaUtil : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        public NLua.Lua LuaEnvironment { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LuaUtil class
        /// </summary>
        /// <param name="lua"></param>
        public LuaUtil(NLua.Lua lua)
        {
            LuaEnvironment = lua;
        }

        /// <summary>
        /// Converts the specified table to an object array
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        [LibraryFunction("TableToArray")]
        public object[] TableToArray(LuaTable table)
        {
            // First of all, check it's actually an array
            int size;
            if (!table.IsArray(out size))
            {
                throw new InvalidOperationException("Specified table is not an array");
            }

            // Get the length
            var arr = new object[size];

            // Create the array
            foreach (var key in table.Keys)
            {
                var index = Convert.ToInt32(key) - 1;
                arr[index] = table[key];
            }

            // Return it
            return arr;
        }

        /// <summary>
        /// Translates Lua Table to Dict
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        [LibraryFunction("TableToLangDict")]
        public object TableToLangDict(LuaTable table)
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in table.Keys)
            {
                if (key is string)
                    dict.Add((string)key, (string)table[key]);
            }
            return dict;
        }

        /// <summary>
        /// Bitwise Or the specified table elements
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        [LibraryFunction("BitwiseOr")]
        public object BitwiseOr(LuaTable table)
        {
            // First of all, check it's actually an array
            int size;
            if (!table.IsArray(out size) || size == 0)
                throw new InvalidOperationException("Specified table is not an array");

            // Get the length
            var result = -1;
            Type type = null;

            // Create the array
            foreach (var key in table.Keys)
            {
                if (result < 0)
                {
                    result = (int)table[key];
                    type = table[key].GetType();
                    continue;
                }
                result |= (int)table[key];
            }

            // Return it
            return Enum.ToObject(type, result);
        }

        /// <summary>
        /// Converts the specified object to the specified type and sets it on the array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        [LibraryFunction("ConvertAndSetOnArray")]
        public bool ConvertAndSetOnArray(object[] array, int index, object value, Type type)
        {
            object converted;
            try
            {
                converted = Convert.ChangeType(value, type);
            }
            catch (Exception)
            {
                return false;
            }
            array[index] = converted;
            return true;
        }

        /// <summary>
        /// Evaluates the specified IEnumerable and converts it to a Lua table
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [LibraryFunction("EvaluateEnumerable")]
        public LuaTable EvaluateEnumerable(IEnumerable obj)
        {
            LuaEnvironment.NewTable("_tmp_enumerable");
            var tbl = LuaEnvironment["_tmp_enumerable"] as LuaTable;
            var e = obj.GetEnumerator();
            var i = 0;
            while (e.MoveNext())
                tbl[++i] = e.Current;
            LuaEnvironment["_tmp_enumerable"] = null;
            return tbl;
        }

        /// <summary>
        /// Specializes the specified generic type
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="argTable"></param>
        /// <returns></returns>
        [LibraryFunction("SpecializeType")]
        public Type SpecializeType(Type baseType, LuaTable argTable)
        {
            int cnt;
            if (!argTable.IsArray(out cnt)) throw new ArgumentException("Table is not an array", "argTable");
            var typeArgs = new Type[cnt];
            for (var i = 0; i < cnt; i++)
            {
                var obj = argTable[i + 1];
                if (obj is LuaTable) obj = (obj as LuaTable)["_type"];
                if (!(obj is Type)) throw new ArgumentException("Item in table is not a Type", $"argTable[{i + 1}]");
                typeArgs[i] = obj as Type;
            }
            return baseType.MakeGenericType(typeArgs);
        }
    }
}
