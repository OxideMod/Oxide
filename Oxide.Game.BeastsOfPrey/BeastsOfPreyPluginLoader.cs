using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.BeastsOfPrey
{
    /// <summary>
    /// Responsible for loading Beasts of Prey core plugins
    /// </summary>
    public class BeastsOfPreyPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof (BeastsOfPreyCore) };
    }
}
