using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.Hurtworld
{
    /// <summary>
    /// Responsible for loading Hurtworld core plugins
    /// </summary>
    public class HurtworldPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(HurtworldCore) };
    }
}
