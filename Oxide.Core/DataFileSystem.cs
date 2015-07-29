using System;
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
        public static void CreateSubdirectory(string name)
        {
            string directoryPath = Path.Combine(Directory, SanitiseName(name));
            CheckPath(directoryPath);
            
            System.IO.Directory.CreateDirectory(directoryPath);
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
                datafile = new DynamicConfigFile();
                _datafiles.Add(name, datafile);
            }

            // Generate the filename
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));

            // Does it exist?
            if (File.Exists(filename))
            {
                // Load it
                datafile.Load(filename);
            }
            else
            {
                // Just make a new one
                datafile.Save(filename);
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

            // Generate the filename
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));

            // Save it
            datafile.Save(filename);
        }

        public T ReadObject<T>(string name)
        {
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));
            CheckPath(filename);
            T customObject;
            if (File.Exists(filename))
            {
                customObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(filename), _settings);
            }
            else
            {
                customObject = (T)Activator.CreateInstance(typeof(T));
                WriteObject(name, customObject);
            }
            return customObject;
        }

        public void WriteObject<T>(string name, T Object)
        {
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));
            CheckPath(filename);
            File.WriteAllText(filename, JsonConvert.SerializeObject(Object, Formatting.Indented, _settings));
        }

        /// <summary>
        /// Check if file path is in chroot directory
        /// </summary>
        /// <param name="filename"></param>
        private void CheckPath(string filename)
        {
            string path = Path.GetFullPath(filename);
            if (!path.StartsWith(Directory, StringComparison.Ordinal))
                throw new Exception("Only access to oxide directory!");
        }
    }
}
