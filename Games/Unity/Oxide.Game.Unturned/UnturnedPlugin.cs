using Oxide.Core;
using Oxide.Game.Unturned.Libraries;

namespace Oxide.Plugins
{
    public abstract class UnturnedPlugin : CSharpPlugin
    {
        protected Unturned unturned;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            unturned = Interface.Oxide.GetLibrary<Unturned>();
        }
    }
}
