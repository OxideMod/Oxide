using Oxide.Core;

using Oxide.Game.HideHoldOut.Libraries;

namespace Oxide.Plugins
{
    public abstract class HideHoldOutPlugin : CSharpPlugin
    {
        protected HideHoldOut HideHoldOut;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            HideHoldOut = Interface.Oxide.GetLibrary<HideHoldOut>();
        }
    }
}
