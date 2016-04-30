using System.Linq;

namespace Oxide.Plugins
{
    [Info("CovalenceSamplePlugin", "Oxide Team", 0.1)]
    [Description("Sample plugin for Covalence")]

    class CovalenceSamplePlugin : CovalencePlugin
    {
        void Loaded()
        {
            Log($"Hello {game}, I am {server.Name}. I know {players.All.Count()} total players, {players.Online.Count()} are currently online.");
        }
    }
}
