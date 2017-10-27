using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.PlanetExplorers
{
    /// <summary>
    /// Responsible for loading Planet Explorers core plugins
    /// </summary>
    public class PlanetExplorersPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(PlanetExplorersCore) };
    }
}
