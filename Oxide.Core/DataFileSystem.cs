using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Oxide.Core.Configuration;

namespace Oxide.Core
{
    /// <summary>
    /// Manages all data files
    /// </summary>
    public class DataFileSystem
    {
        /// <summary>
        /// Gets the directory that this system works in
        /// </summary>
        public string Directory { get; private set; }

        // All currently loaded datafiles
        private readonly Dictionary<string, DynamicConfigFile> _datafiles;

        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Initializes a new instance of the DataFileSystem class
        /// </summary>
        /// <param name="directory"></param>
        public DataFileSystem(string directory)
        {
            Directory = directory;
            _datafiles = new Dictionary<string, DynamicConfigFile>();
            var converter = new KeyValuesConverter();
            _settings = new JsonSerializerSettings();
            _settings.Converters.Add(converter);
        }

        /// <summary>
        /// Checks if data file exists without creating one
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool DatafileExists(string name)
        {
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));
            CheckPath(filename);
            
            if (File.Exists(filename))
            {
                return true;   
            }
            
            return false;
        }
        
        /// <summary>
        /// Allows you to create a subdirectory
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataFileSystem CreateSubdirectory(string name)
        {
            string directoryPath = Path.Combine(Directory, SanitiseName(name));
            CheckPath(directoryPath);
            
            System.IO.Directory.CreateDirectory(directoryPath);
            
            return new DataFileSystem(directoryPath);
        }

        /// <summary>
        /// Makes the specified name safe for use in a filename
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string SanitiseName(string name)
        {
            return Regex.Replace(name, @"[/:,\\]", "_");
        }

        /// <summary>
        /// Check if datafile exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ExistsDatafile(string name)
        {
            DynamicConfigFile datafile;
            if (!_datafiles.TryGetValue(name, out datafile))
            {
                datafile = new DynamicConfigFile(Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name))));
                _datafiles.Add(name, datafile);
            }
            return datafile.Exists();
        }

        /// <summary>
        /// Gets a datafile
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DynamicConfigFile GetDatafile(string name)
        {
            // See if it already exists
            DynamicConfigFile datafile;
            if (!_datafiles.TryGetValue(name, out datafile))
            {
                datafile = new DynamicConfigFile(Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name))));
                _datafiles.Add(name, datafile);
            }

            // Does it exist?
            if (datafile.Exists())
            {
                // Load it
                datafile.Load();
            }
            else
            {
                // Just make a new one
                datafile.Save();
            }

            return datafile;
        }

        /// <summary>
        /// Saves the specified datafile
        /// </summary>
        /// <param name="name"></param>
        public void SaveDatafile(string name)
        {
            // Get the datafile
            DynamicConfigFile datafile;
            if (!_datafiles.TryGetValue(name, out datafile)) return;
            // Save it
            datafile.Save();
        }

        public T ReadObject<T>(string name)
        {
            var datafile = GetDatafile(name);
            return datafile.ReadObject<T>();
        }

        public void WriteObject<T>(string name, T Object, bool sync = false)
        {
            var datafile = GetDatafile(name);
            datafile.WriteObject(Object, sync);
        }
    }
}
