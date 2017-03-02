using System.Text;
using Newtonsoft.Json;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents all "root" Oxide config settings
    /// </summary>
    public class OxideConfig : ConfigFile
    {
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
        /// Gets or sets information regarding the Oxide Console
        /// </summary>
        [JsonProperty(PropertyName = "OxideConsole")]
        public OxideConsole Console { get; set; }

        /// <summary>
        /// Gets or sets the default permissions group for new players
        /// </summary>
        [JsonIgnore] // Ignored for now until this is implemented
        public string DefaultGroup { get; set; }

        /// <summary>
        /// Gets or sets the command line arguments to search for the instance directory
        /// </summary>
        public string[] InstanceCommandLines { get; set; }

        /// <summary>
        /// Gets or sets the directory to find plugin config files (relative to the instance path)
        /// </summary>
        public string ConfigDirectory { get; set; }

        /// <summary>
        /// Sets defaults for Oxide configuration
        /// </summary>
        public OxideConfig(string filename) : base(filename)
        {
            ConfigDirectory = "config";
            DefaultGroup = "default";
            Console = new OxideConsole { Enabled = true, MinimalistMode = true, ShowStatusBar = true, ShowStacktraces = true};
        }

        /// <summary>
        /// Gets argument data for the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="varname"></param>
        /// <param name="format"></param>
        public void GetInstanceCommandLineArg(int index, out string varname, out string format)
        {
            // Format is "folder/{variable}/otherfolder"
            var cmd = InstanceCommandLines[index];
            StringBuilder varnamesb = new StringBuilder(), formatsb = new StringBuilder();
            var invar = 0;
            foreach (var c in cmd)
            {
                switch (c)
                {
                    case '{':
                        invar++;
                        break;
                    case '}':
                        invar--;
                        if (invar == 0) formatsb.Append("{0}");
                        break;
                    default:
                        if (invar == 0)
                            formatsb.Append(c);
                        else
                            varnamesb.Append(c);
                        break;
                }
            }
            varname = varnamesb.ToString();
            format = formatsb.ToString();
        }
    }
}
