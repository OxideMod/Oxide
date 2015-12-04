using Oxide.Core;

using Oxide.Game.FortressCraft.Libraries;

namespace Oxide.Plugins
{
    public abstract class FortressCraftPlugin : CSharpPlugin
    {
        protected FortressCraft fortress;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            fortress = Interface.Oxide.GetLibrary<FortressCraft>();
        }
    }
}
