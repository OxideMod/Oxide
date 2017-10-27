using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.ReignOfKings
{
    /// <summary>
    /// Responsible for loading Reign of Kings core plugins
    /// </summary>
    public class ReignOfKingsPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(ReignOfKingsCore) };
    }
}
