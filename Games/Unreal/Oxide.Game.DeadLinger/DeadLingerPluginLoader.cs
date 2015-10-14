using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.DeadLinger
{
    /// <summary>
    /// Responsible for loading The Dead Linger core plugins
    /// </summary>
    public class DeadLingerPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(DeadLingerCore) };
    }
}
