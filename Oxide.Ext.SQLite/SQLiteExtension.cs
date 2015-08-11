using System;
using System.IO;
using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Ext.SQLite
{
    public class SQLiteExtension : Extension
    {
        public SQLiteExtension(ExtensionManager manager) : base(manager)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var extDir = Interface.Oxide.ExtensionDirectory;
                File.WriteAllText(Path.Combine(extDir, "System.Data.SQLite.dll.config"), $"<configuration>\n<dllmap dll=\"sqlite3\" target=\"{extDir}/x86/libsqlite3.so\" os=\"linux\" cpu=\"x86\" />\n<dllmap dll=\"sqlite3\" target=\"{extDir}/x64/libsqlite3.so\" os=\"linux\" cpu=\"x86-64\" />\n</configuration>");
            }
        }

        public override string Name => "SQLite";

        public override VersionNumber Version => new VersionNumber(1, 0, OxideMod.Version.Patch);

        public override string Author => "Oxide Team";

        private Libraries.SQLite _sqlite;

        public override void Load()
        {
            Manager.RegisterLibrary("SQLite", _sqlite = new Libraries.SQLite());
        }

        public override void LoadPluginWatchers(string plugindir)
        {
        }

        public override void OnModLoad()
        {
        }

        public override void OnShutdown()
        {
            _sqlite?.Shutdown();
        }
    }
}
