using Oxide.Core;

using Oxide.Game.FromTheDepths.Libraries;

namespace Oxide.Plugins
{
    public abstract class FromTheDepthsPlugin : CSharpPlugin
    {
        protected FromTheDepths ftd;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            ftd = Interface.Oxide.GetLibrary<FromTheDepths>();
        }
    }
}
