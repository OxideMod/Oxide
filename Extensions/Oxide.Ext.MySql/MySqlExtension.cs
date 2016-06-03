using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Ext.MySql
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class MySqlExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "MySql";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        private Libraries.MySql mySql;

        /// <summary>
        /// Initializes a new instance of the MySqlExtension class
        /// </summary>
        public MySqlExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load() => Manager.RegisterLibrary("MySql", mySql = new Libraries.MySql());

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public override void LoadPluginWatchers(string pluginDirectory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
        }
    }
}
