using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.Unturned
{
    /// <summary>
    /// Responsible for loading core plugins for "Unturned"
    /// </summary>
    public class UnturnedPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(UnturnedCore) };
    }
}
