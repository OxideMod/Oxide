using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.Nomad
{
    /// <summary>
    /// Responsible for loading Nomad core plugins
    /// </summary>
    public class NomadPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(NomadCore) };
    }
}
