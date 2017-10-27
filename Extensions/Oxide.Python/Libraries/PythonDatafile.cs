using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using System.Collections.Generic;

namespace Oxide.Core.Python.Libraries
{
    /// <summary>
    /// A datafile library that allows Python to access datafiles
    /// </summary>
    public class PythonDatafile : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets the Python engine
        /// </summary>
        public ScriptEngine PythonEngine { get; }

        // The data file map
        private readonly Dictionary<DynamicConfigFile, PythonDictionary> datafilemap;

        /// <summary>
        /// Initializes a new instance of the PythonDatafile class
        /// <param name="engine"></param>
        /// </summary>
        public PythonDatafile(ScriptEngine engine)
        {
            datafilemap = new Dictionary<DynamicConfigFile, PythonDictionary>();
            PythonEngine = engine;
        }

        /// <summary>
        /// Gets a datatable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [LibraryFunction("GetData")]
        public PythonDictionary GetData(string name)
        {
            // Get the data file
            DynamicConfigFile datafile = Interface.Oxide.DataFileSystem.GetDatafile(name);
            if (datafile == null) return null;

            // Check if it already exists
            PythonDictionary dict;
            if (datafilemap.TryGetValue(datafile, out dict)) return dict;

            // Create the table
            dict = Utility.DictionaryFromConfig(datafile, PythonEngine);
            datafilemap.Add(datafile, dict);

            // Return
            return dict;
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
            PythonDictionary dict;
            if (!datafilemap.TryGetValue(datafile, out dict)) return;

            // Copy and save
            Utility.SetConfigFromDictionary(datafile, dict);
            Interface.Oxide.DataFileSystem.SaveDatafile(name);
        }
    }
}
