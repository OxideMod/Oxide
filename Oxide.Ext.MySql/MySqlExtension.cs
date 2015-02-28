using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Ext.MySql
{
    public class MySqlExtension : Extension
    {
        public MySqlExtension(ExtensionManager manager) : base(manager)
        {
        }

        public override string Name { get { return "MySql"; } }

        public override VersionNumber Version { get { return new VersionNumber(1, 0, OxideMod.Version.Patch); } }

        public override string Author { get { return "Nogrod"; } }

        public override void Load()
        {
            Manager.RegisterLibrary("MySql", new Libraries.MySql());
        }

        public override void LoadPluginWatchers(string plugindir)
        {
        }

        public override void OnModLoad()
        {
        }
    }
}
