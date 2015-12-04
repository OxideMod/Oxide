using Oxide.Core;

using Oxide.Game.BeastsOfPrey.Libraries;

namespace Oxide.Plugins
{
    public abstract class BeastsOfPreyPlugin : CSharpPlugin
    {
        protected BeastsOfPrey bop;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            bop = Interface.Oxide.GetLibrary<BeastsOfPrey>();
        }
    }
}
