using Oxide.Core.Plugins;
using System;

namespace Oxide.Core.Unity.Plugins
{
    /// <summary>
    /// Responsible for loading Unity core plugins
    /// </summary>
    public class UnityPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(UnityCore) };
    }
}
