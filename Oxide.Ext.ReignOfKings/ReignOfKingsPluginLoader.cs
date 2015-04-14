using System;

using Oxide.Core.Plugins;

namespace Oxide.ReignOfKings.Plugins
{
    /// <summary>
    /// Responsible for loading Reign of Kings core plugins
    /// </summary>
    public class ReignOfKingsPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(ReignOfKingsCore) };
    }
}
