using System;

using Oxide.Core.Plugins;

namespace Oxide.Rust.Plugins
{
    /// <summary>
    /// Responsible for loading Rust core plugins
    /// </summary>
    public class RustPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(RustCore) };
    }
}
