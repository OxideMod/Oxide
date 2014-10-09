using System;
using System.IO;

using Newtonsoft.Json;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents a config file
    /// </summary>
    public abstract class ConfigFile
    {
        /// <summary>
        /// Loads a config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public static T Load<T>(string filename) where T : ConfigFile
        {
            T config = Activator.CreateInstance<T>();
            config.Load(filename);
            return config;
        }

        /// <summary>
        /// Loads this config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Load(string filename)
        {
            string source = File.ReadAllText(filename);
            JsonConvert.PopulateObject(source, this);
        }

        /// <summary>
        /// Saves this config to the specified file
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Save(string filename)
        {
            string source = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filename, source);
        }
    }
}
