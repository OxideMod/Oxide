using Oxide.Core;

namespace Oxide.Plugins
{
    public abstract class TheForestPlugin : CSharpPlugin
    {
        protected Game.TheForest.Libraries.TheForest forest;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            forest = Interface.Oxide.GetLibrary<Game.TheForest.Libraries.TheForest>();
        }
    }
}
