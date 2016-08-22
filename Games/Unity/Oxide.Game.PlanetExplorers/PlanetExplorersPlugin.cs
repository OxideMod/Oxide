using Oxide.Core;
using Oxide.Game.PlanetExplorers.Libraries;

namespace Oxide.Plugins
{
    public abstract class PlanetExplorersPlugin : CSharpPlugin
    {
        protected PlanetExplorers planetex;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            planetex = Interface.Oxide.GetLibrary<PlanetExplorers>("PlanetExplorers");
        }
    }
}
