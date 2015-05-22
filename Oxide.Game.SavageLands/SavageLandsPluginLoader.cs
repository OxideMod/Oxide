using System;

using Oxide.Core.Plugins;

namespace Oxide.Game.SavageLands
{
    /// <summary>
    /// Responsible for loading Savage Lands core plugins
    /// </summary>
    public class SavageLandsPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(SavageLandsCore) };
    }
}
