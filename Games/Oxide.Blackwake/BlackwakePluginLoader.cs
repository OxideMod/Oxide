using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.Blackwake
{
    /// <summary>
    /// Responsible for loading core plugins for "Blackwake"
    /// </summary>
    public class BlackwakePluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(BlackwakeCore) };
    }
}
