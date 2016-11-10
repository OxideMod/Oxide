using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.MedievalEngineers
{
    /// <summary>
    /// Responsible for loading Medieval Engineers core plugins
    /// </summary>
    public class MedievalEngineersPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(MedievalEngineersCore) };
    }
}
