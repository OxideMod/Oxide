using System;
using Oxide.Core.Plugins;

namespace Oxide.Game.HideHoldOut
{
    /// <summary>
    /// Responsible for loading HideHoldOut core plugins
    /// </summary>
    public class HideHoldOutPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(HideHoldOutCore) };
    }
}
