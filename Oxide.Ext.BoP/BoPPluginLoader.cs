using System;

using Oxide.Core.Plugins;

namespace Oxide.BoP.Plugins
{
    /// <summary>
    /// Responsible for loading The Forest core plugins
    /// </summary>
    public class BoPPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins
        {
            get { return new[] {typeof (BoPCore)}; }
        }
    }
}
