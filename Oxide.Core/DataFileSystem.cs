using System.Collections.Generic;
using System.IO;

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
        private Dictionary<string, DynamicConfigFile> datafiles;

        /// <summary>
        /// Initializes a new instance of the DataFileSystem class
        /// </summary>
        /// <param name="directory"></param>
        public DataFileSystem(string directory)
        {
            Directory = directory;
            datafiles = new Dictionary<string, DynamicConfigFile>();
        }

        /// <summary>
        /// Makes the specified name safe for use in a filename
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string SanitiseName(string name)
        {
            string file = name.Replace('\\', '_');
            file = file.Replace('/', '_');
            file = file.Replace(':', '_');
            file = file.Replace(',', '_');
            return file;
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
            if (datafiles.TryGetValue(name, out datafile)) return datafile;

            // Generate the filename
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));

            // Does it exist?
            if (File.Exists(filename))
            {
                // Load it
                datafile = new DynamicConfigFile();
                datafile.Load(filename);
            }
            else
            {
                // Just make a new one
                datafile = new DynamicConfigFile();
                datafile.Save(filename);
            }

            // Add and return
            datafiles.Add(name, datafile);
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
            if (!datafiles.TryGetValue(name, out datafile)) return;

            // Generate the filename
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));

            // Save it
            datafile.Save(filename);
        }

        public T ReadObject<T>(string name)
        {
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));
            T CustomObject = default(T);
            if (File.Exists(filename) == false)
            {
                throw new FileNotFoundException(string.Format("File not found : {0}",filename));
            }
            try
            {
                CustomObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(filename).ToString());
            }
            catch (JsonSerializationException)
            {
                throw new JsonSerializationException(string.Format("Deserialization error on : {0}",filename));
            }
            return CustomObject;
        }

        public void WriteObject<T>(string name, T Object)
        {
            string filename = Path.Combine(Directory, string.Format("{0}.json", SanitiseName(name)));
            string JsonText = JsonConvert.SerializeObject(Object,Formatting.Indented);
            File.WriteAllText(filename, JsonText);
        }
    }
}
