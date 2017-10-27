using Jint;
using Jint.Native.Object;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using System.Collections.Generic;

namespace Oxide.Core.JavaScript.Libraries
{
    /// <summary>
    /// A datafile library that allows JavaScript to access datafiles
    /// </summary>
    public class JavaScriptDatafile : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets the JavaScript engine
        /// </summary>
        public Engine JavaScriptEngine { get; }

        // The data file map
        private readonly Dictionary<DynamicConfigFile, ObjectInstance> datafilemap;

        /// <summary>
        /// Initializes a new instance of the LuaDatafile class
        /// <param name="engine"></param>
        /// </summary>
        public JavaScriptDatafile(Engine engine)
        {
            datafilemap = new Dictionary<DynamicConfigFile, ObjectInstance>();
            JavaScriptEngine = engine;
        }

        /// <summary>
        /// Gets a datatable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("GetData")]
        public ObjectInstance GetData(string name)
        {
            // Get the data file
            DynamicConfigFile datafile = Interface.Oxide.DataFileSystem.GetDatafile(name);
            if (datafile == null) return null;

            // Check if it already exists
            ObjectInstance obj;
            if (datafilemap.TryGetValue(datafile, out obj)) return obj;

            // Create the table
            obj = Utility.ObjectFromConfig(datafile, JavaScriptEngine);
            datafilemap.Add(datafile, obj);

            // Return
            return obj;
        }

        /// <summary>
        /// Saves a datatable
        /// </summary>
        /// <param name="name"></param>
        [LibraryFunction("SaveData")]
        public void SaveData(string name)
        {
            // Get the data file
            DynamicConfigFile datafile = Interface.Oxide.DataFileSystem.GetDatafile(name);
            if (datafile == null) return;

            // Get the table
            ObjectInstance obj;
            if (!datafilemap.TryGetValue(datafile, out obj)) return;

            // Copy and save
            Utility.SetConfigFromObject(datafile, obj);
            Interface.Oxide.DataFileSystem.SaveDatafile(name);
        }
    }
}
