using NLua;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using System.Collections.Generic;

namespace Oxide.Core.Lua.Libraries
{
    /// <summary>
    /// A datafile library that allows Lua to access datafiles
    /// </summary>
    public class LuaDatafile : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets the Lua environment
        /// </summary>
        public NLua.Lua LuaEnvironment { get; }

        // The data file map
        private Dictionary<DynamicConfigFile, LuaTable> datafilemap;

        /// <summary>
        /// Initializes a new instance of the LuaDatafile class
        /// </summary>
        /// <param name="lua"></param>
        public LuaDatafile(NLua.Lua lua)
        {
            datafilemap = new Dictionary<DynamicConfigFile, LuaTable>();
            LuaEnvironment = lua;
        }

        /// <summary>
        /// Gets a datatable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("GetDataTable")]
        public LuaTable GetDataTable(string name)
        {
            // Get the data file
            var datafile = Interface.Oxide.DataFileSystem.GetDatafile(name);
            if (datafile == null) return null;

            // Check if it already exists
            LuaTable table;
            if (datafilemap.TryGetValue(datafile, out table))
            {
                table.Dispose();
                //return table;
            }

            // Create the table
            table = Utility.TableFromConfig(datafile, LuaEnvironment);
            datafilemap[datafile] = table;

            // Return
            return table;
        }

        /// <summary>
        /// Saves a datatable
        /// </summary>
        /// <param name="name"></param>
        [LibraryFunction("SaveDataTable")]
        public void SaveDataTable(string name)
        {
            // Get the data file
            var datafile = Interface.Oxide.DataFileSystem.GetDatafile(name);
            if (datafile == null) return;

            // Get the table
            LuaTable table;
            if (!datafilemap.TryGetValue(datafile, out table)) return;

            // Copy and save
            Utility.SetConfigFromTable(datafile, table);
            Interface.Oxide.DataFileSystem.SaveDatafile(name);
        }
    }
}
