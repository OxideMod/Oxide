using System;
using System.Collections;

using NLua;

using Oxide.Core.Libraries;

namespace Oxide.Ext.Lua.Libraries
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
        /// <param name="logger"></param>
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
            object[] arr = new object[size];

            // Create the array
            foreach (object key in table.Keys)
            {
                int index = Convert.ToInt32(key) - 1;
                arr[index] = table[key];
            }

            // Return it
            return arr;
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
            LuaTable tbl = LuaEnvironment["_tmp_enumerable"] as LuaTable;
            var e = obj.GetEnumerator();
            int i = 0;
            while (e.MoveNext())
            {
                tbl[++i] = e.Current;
            }
            LuaEnvironment["_tmp_enumerable"] = null;
            return tbl;
        }

        /// <summary>
        /// Specialises the specified generic type
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="argTable"></param>
        /// <returns></returns>
        [LibraryFunction("SpecialiseType")]
        public Type SpecialiseType(Type baseType, LuaTable argTable)
        {
            int cnt;
            if (!argTable.IsArray(out cnt)) throw new ArgumentException("Table is not an array", "argTable");
            Type[] typeArgs = new Type[cnt];
            for (int i = 0; i < cnt; i++)
            {
                object obj = argTable[i + 1];
                if (obj is LuaTable) obj = (obj as LuaTable)["_type"];
                if (!(obj is Type)) throw new ArgumentException("Item in table is not a Type", $"argTable[{i + 1}]");
                typeArgs[i] = obj as Type;
            }
            return baseType.MakeGenericType(typeArgs);
        }
    }
}
