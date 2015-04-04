using System;

using Oxide.Core.Plugins;

namespace Oxide.RustLegacy.Plugins
{
    /// <summary>
    /// Responsible for loading Rust core plugins
    /// </summary>
    public class RustLegacyPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(RustLegacyCore) };
    }
}
