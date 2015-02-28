using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Ext.SQLite
{
    public class SQLiteExtension : Extension
    {
        public SQLiteExtension(ExtensionManager manager) : base(manager)
        {
        }

        public override string Name { get { return "SQLite"; } }

        public override VersionNumber Version { get { return new VersionNumber(1, 0, OxideMod.Version.Patch); } }

        public override string Author { get { return "Nogrod"; } }

        public override void Load()
        {
            Manager.RegisterLibrary("SQLite", new Libraries.SQLite());
        }

        public override void LoadPluginWatchers(string plugindir)
        {
        }

        public override void OnModLoad()
        {
        }
    }
}
