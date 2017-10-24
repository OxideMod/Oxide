using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.FromTheDepths
{
    /// <summary>
    /// Responsible for loading From the Depths core plugins
    /// </summary>
    public class FromTheDepthsPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(FromTheDepthsCore) };
    }
}
