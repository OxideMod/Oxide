using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Rust2Text", "ThermalCube", 0.1)]
    class Rust2Text : RustPlugin
    {
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("[TimeVote] Creating config file");
            Config.Clear();

            Config.Settings.Formatting = Newtonsoft.Json.Formatting.Indented;
            Config.Set("url", "https://stream.watsonplatform.net/speech-to-text/api");
            Config.Set("credentials", "username", "da6d8184-aefd-4c41-963e-c61b64fdf3d0");
            Config.Set("credentials", "password", "iAo7eDboi8HS");
            
            SaveConfig();
            string[] sleeping = BasePlayer.activePlayerList.Where(p => p.IsSleeping()).Select(p => p.displayName).ToArray();
            string msg = "Players sleeping are: " + string.Join(", ", sleeping);
        }
        
    }
}