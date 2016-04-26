using Oxide.Core;

using Oxide.Game.SavageLands.Libraries;

namespace Oxide.Plugins
{
    public abstract class SavageLandsPlugin : CSharpPlugin
    {
        protected SavageLands savage;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            savage = Interface.Oxide.GetLibrary<SavageLands>("Savage");
        }
    }
}
