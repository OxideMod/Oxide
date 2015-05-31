using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.Blockstorm
{
    /// <summary>
    /// Responsible for loading Blockstorm core plugins
    /// </summary>
    public class BlockstormPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(BlockstormCore) };
    }
}
