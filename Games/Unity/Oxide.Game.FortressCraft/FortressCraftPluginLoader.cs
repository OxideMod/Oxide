using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.FortressCraft
{
    /// <summary>
    /// Responsible for loading FortressCraft core plugins
    /// </summary>
    public class FortressCraftPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(FortressCraftCore) };
    }
}
