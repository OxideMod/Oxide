using System.Linq;

namespace Oxide.Plugins
{
    [Info("Sample Covalence Plugin", "Oxide Team", 0.1)]
    class CovalenceSamplePlugin : CovalencePlugin
    {
        void Loaded()
        {
            Log($"Hello {game}, I am {server.Name}. I know {players.All.Count()} total players, {players.Online.Count()} are currently online.");
        }
    }
}
