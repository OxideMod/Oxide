using System.Text;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents all "root" Oxide config settings
    /// </summary>
    public class OxideConfig : ConfigFile
    {
        /// <summary>
        /// Gets or sets the directory to find extensions (relative to the startup path)
        /// </summary>
        public string ExtensionDirectory { get; set; }

        /// <summary>
        /// Gets or sets the default permissions group for new players
        /// </summary>
        public string DefaultGroup { get; set; }

        /// <summary>
        /// Gets or sets if the Oxide console should be setup
        /// </summary>
        public bool CustomConsole { get; set; }

        /// <summary>
        /// Gets or sets the command line arguments to search for the instance directory
        /// </summary>
        public string[] InstanceCommandLines { get; set; }

        /// <summary>
        /// Sets defaults for Oxide configuration
        /// </summary>
        public OxideConfig(string filename) : base(filename)
        {
            DefaultGroup = "default";
            CustomConsole = false;
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
