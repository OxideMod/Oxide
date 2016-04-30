using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.BeastsOfPrey
{
    /// <summary>
    /// Responsible for loading core plugins for "Beasts of Prey"
    /// </summary>
    public class BeastsOfPreyPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof (BeastsOfPreyCore) };
    }
}
