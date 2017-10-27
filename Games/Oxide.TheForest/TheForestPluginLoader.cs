using Oxide.Core.Plugins;
using System;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// Responsible for loading core plugins for "The Forest"
    /// </summary>
    public class TheForestPluginLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(TheForestCore) };
    }
}
