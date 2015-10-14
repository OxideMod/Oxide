using System;

using Oxide.Core.Plugins;

namespace Oxide.Ext.Unity.Plugins
{
    /// <summary>
    /// Responsible for loading Unity core plugins
    /// </summary>
    public class UnityPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(UnityCore) };
    }
}
