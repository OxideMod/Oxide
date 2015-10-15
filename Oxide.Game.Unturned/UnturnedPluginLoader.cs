using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.Unturned
{
    /// <summary>
    /// Responsible for loading Unturned core plugins
    /// </summary>
    public class UnturnedPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(UnturnedCore) };
    }
}
