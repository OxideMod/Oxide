using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Ext.MySql
{
    public class MySqlExtension : Extension
    {
        public MySqlExtension(ExtensionManager manager) : base(manager)
        {
        }

        public override string Name => "MySql";

        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        public override string Author => "Oxide Team";

        private Libraries.MySql _mySql;

        public override void Load()
        {
            Manager.RegisterLibrary("MySql", _mySql = new Libraries.MySql());
        }

        public override void LoadPluginWatchers(string plugindir)
        {
        }

        public override void OnModLoad()
        {
        }

        public override void OnShutdown()
        {
            _mySql?.Shutdown();
        }
    }
}
