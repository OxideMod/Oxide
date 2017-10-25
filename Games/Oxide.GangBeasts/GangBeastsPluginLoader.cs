using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.GangBeasts
{
    /// <summary>
    /// Responsible for loading core plugins for "Gang Beasts"
    /// </summary>
    public class GangBeastsPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(GangBeastsCore) };
    }
}
