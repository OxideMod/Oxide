using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.Terraria
{
    /// <summary>
    /// Responsible for loading Terraria core plugins
    /// </summary>
    public class TerrariaPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(TerrariaCore) };
    }
}
