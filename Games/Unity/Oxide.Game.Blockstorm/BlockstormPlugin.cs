using Oxide.Core;

using Oxide.Game.Blockstorm.Libraries;

namespace Oxide.Plugins
{
    public abstract class BlockstormPlugin : CSharpPlugin
    {
        protected Blockstorm blockstorm;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            blockstorm = Interface.Oxide.GetLibrary<Blockstorm>("Blockstorm");
        }
    }
}
