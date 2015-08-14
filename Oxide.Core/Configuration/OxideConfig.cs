using System.Text;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents all "root" oxide config settings
    /// </summary>
    public class OxideConfig : ConfigFile
    {
        /// <summary>
        /// Gets or sets the directory to find extensions (relative to the startup path)
        /// </summary>
        public string ExtensionDirectory { get; set; }

        /// <summary>
        /// Gets or sets the directory to find plugins (relative to the instance path)
        /// </summary>
        public string PluginDirectory { get; set; }

        /// <summary>
        /// Gets or sets the directory to find plugin config files (relative to the instance path)
        /// </summary>
        public string ConfigDirectory { get; set; }

        /// <summary>
        /// Gets or sets the directory to find plugin data files (relative to the instance path)
        /// </summary>
        public string DataDirectory { get; set; }

        /// <summary>
        /// Gets or sets the directory to find log files (relative to the instance path)
        /// </summary>
        public string LogDirectory { get; set; }

        /// <summary>
        /// Gets or sets the command line arguments to search for the instance directory
        /// </summary>
        public string[] InstanceCommandLines { get; set; }

        /// <summary>
        /// Gets or sets if a console should be setup
        /// </summary>
        public bool DisableConsole { get; set; }

        /// <summary>
        /// Sets defaults for oxide configuration
        /// </summary>
        public OxideConfig(string filename) : base(filename)
        {
            PluginDirectory = "plugins";
            ConfigDirectory = "config";
            DataDirectory = "data";
            LogDirectory = "logs";
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
            string cmd = InstanceCommandLines[index];
            StringBuilder varnamesb = new StringBuilder(), formatsb = new StringBuilder();
            int invar = 0;
            for (int i = 0; i < cmd.Length; i++)
            {
                char c = cmd[i];
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
