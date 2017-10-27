extern alias Oxide;

using Oxide::Newtonsoft.Json;
using System;
using System.IO;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents a config file
    /// </summary>
    public abstract class ConfigFile
    {
        [JsonIgnore]
        public string Filename { get; private set; }

        protected ConfigFile(string filename)
        {
            Filename = filename;
        }

        /// <summary>
        /// Loads a config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public static T Load<T>(string filename) where T : ConfigFile
        {
            var config = (T)Activator.CreateInstance(typeof(T), filename);
            config.Load();
            return config;
        }

        /// <summary>
        /// Loads this config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Load(string filename = null)
        {
            var source = File.ReadAllText(filename ?? Filename);
            JsonConvert.PopulateObject(source, this);
        }

        /// <summary>
        /// Saves this config to the specified file
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Save(string filename = null)
        {
            var source = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filename ?? Filename, source);
        }
    }
}
