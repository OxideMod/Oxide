extern alias Oxide;

using Oxide::Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents all Oxide config settings
    /// </summary>
    public class OxideConfig : ConfigFile
    {
        /// <summary>
        /// Settings for the modded server
        /// </summary>
        public class OxideOptions
        {
            public bool Modded;
            public DefaultGroups DefaultGroups;
        }

        [JsonObject]
        public class DefaultGroups : IEnumerable<string>
        {
            public string Players;
            public string Administrators;

            public IEnumerator<string> GetEnumerator()
            {
                yield return Players;
                yield return Administrators;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Settings for the custom Oxide console
        /// </summary>
        public class OxideConsole
        {
            /// <summary>
            /// Gets or sets if the Oxide console should be setup
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Gets or sets if the Oxide console should run in minimalist mode (no tags in the console)
            /// </summary>
            public bool MinimalistMode { get; set; }

            /// <summary>
            /// Gets or sets if the Oxide console should show the toolbar on the bottom with server information
            /// </summary>
            public bool ShowStatusBar { get; set; }

            /// <summary>
            /// Gets or sets if the Oxide console should show the stacktrace when an error occurs
            /// </summary>
            public bool ShowStacktraces { get; set; }
        }

        /// <summary>
        /// Settings for the custom Oxide remote console
        /// </summary>
        public class OxideRcon
        {
            /// <summary>
            /// Gets or sets if the Oxide remote console should be setup
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Gets or sets the Oxide remote console port
            /// </summary>
            public int Port { get; set; }

            /// <summary>
            /// Gets or sets the Oxide remote console password
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets the Oxide remote console chat prefix
            /// </summary>
            public string ChatPrefix { get; set; }
        }

        /// <summary>
        /// Gets or sets information regarding the Oxide mod
        /// </summary>
        public OxideOptions Options { get; set; }

        /// <summary>
        /// Gets or sets information regarding the Oxide console
        /// </summary>
        [JsonProperty(PropertyName = "OxideConsole")]
        public OxideConsole Console { get; set; }

        /// <summary>
        /// Gets or sets information regarding the Oxide remote console
        /// </summary>
        [JsonProperty(PropertyName = "OxideRcon")]
        public OxideRcon Rcon { get; set; }

        /// <summary>
        /// Sets defaults for Oxide configuration
        /// </summary>
        public OxideConfig(string filename) : base(filename)
        {
            Options = new OxideOptions { Modded = true, DefaultGroups = new DefaultGroups { Administrators = "admin", Players = "default" } };
            Console = new OxideConsole { Enabled = true, MinimalistMode = true, ShowStatusBar = true, ShowStacktraces = true };
            Rcon = new OxideRcon { Enabled = false, ChatPrefix = "[Server Console]", Port = 25580, Password = string.Empty };
        }
    }
}
