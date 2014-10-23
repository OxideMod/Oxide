using System;
using System.Collections.Generic;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;

using NLua;

namespace Oxide.Lua.Libraries
{
    /// <summary>
    /// A utility library for Lua specific functions
    /// </summary>
    public class LuaUtil : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Converts the specified table to an object array
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        [LibraryFunction("TableToArray")]
        public object[] TableToArray(LuaTable table)
        {
            // First of all, check it's actually an array
            if (!table.IsArray())
            {
                throw new InvalidOperationException("Specified table is not an array");
            }

            // Get the length
            int len = table.Keys.Count;
            object[] arr = new object[len];

            // Create the array
            foreach (object key in table.Keys)
            {
                int index = Convert.ToInt32(key) - 1;
                arr[index] = table[key];
            }

            // Return it
            return arr;
        }
    }
}
