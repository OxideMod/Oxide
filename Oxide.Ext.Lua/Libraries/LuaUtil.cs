using System;

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
    }
}
