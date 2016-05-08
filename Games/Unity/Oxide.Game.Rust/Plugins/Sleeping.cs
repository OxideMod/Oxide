using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Sleeping", "ThermalCube", 0.1)]
    class Sleeping : RustPlugin
    {
        [ChatCommand("sleeping")]
        public void sleeping(BasePlayer ply, string cmd, string[] args)
        {
            string[] sleeping = BasePlayer.activePlayerList.Where(p => p.IsSleeping()).Select(p => p.displayName).ToArray();
            string msg = "Zur Zeit schäft: " + string.Join(", ", sleeping);

            rust.SendChatMessage(ply, "[Sleeping]", msg);
        }
        
    }
}