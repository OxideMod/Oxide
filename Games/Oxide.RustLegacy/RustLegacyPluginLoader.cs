using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// Responsible for loading Rust Legacy core plugins
    /// </summary>
    public class RustLegacyPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(RustLegacyCore) };
    }
}
