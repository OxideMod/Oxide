using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Oxide.Core.Libraries.RemoteConsole
{
    [Serializable]
    public class ConfigFile
    {
        [JsonIgnore]
        public static readonly string ConfigLoc = Path.Combine(Interface.Oxide.ConfigDirectory, "RemoteConsoleConfiguration.json");

        /// <summary>
        /// The Name that is shown in game chat when a message command is recieved
        /// </summary>
        [JsonProperty(PropertyName = "Chat Name", Order = 0)]
        public string ConsoleName = "[ServerConsole]";

        /// <summary>
        /// Remote Console Port
        /// </summary>
        [JsonProperty(PropertyName = "RCON Port", Order = 1)]
        public int Port = 25580;

        /// <summary>
        /// Remote Console Password
        /// </summary>
        [JsonProperty(PropertyName = "RCON Password", Order = 2)]
        public string Password = string.Empty;

        /// <summary>
        /// Loads the config file or creates a new one if it doesn't exist
        /// </summary>
        /// <returns></returns>
        public static ConfigFile LoadConfig()
        {
            ConfigFile file = null;
            if (File.Exists(ConfigLoc))
                JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(ConfigLoc));
            if (file == null)
                return CreateConfig();
            Interface.Oxide.LogInfo("[RCON] Configuration File Loaded");
            return file;
        }

        // Creates the Config File if non existant
        private static ConfigFile CreateConfig()
        {
            ConfigFile file = new ConfigFile();
            file.Password = GeneratePassword(15);

            File.WriteAllText(ConfigLoc, JsonConvert.SerializeObject(file, Formatting.Indented));

            Interface.Oxide.LogInfo("[RCON] New Configuration file created '{0}'", ConfigLoc);

            return file;
        }

        // Generates a random AlphaNumeric Password
        private static string GeneratePassword(int length)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new System.Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}