using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Ext.SQLite
{
    public class SQLiteExtension : Extension
    {
        public SQLiteExtension(ExtensionManager manager) : base(manager)
        {
        }

        public override string Name => "SQLite";

        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        public override string Author => "Oxide Team";

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
