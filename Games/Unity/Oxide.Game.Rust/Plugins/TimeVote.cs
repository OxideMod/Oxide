using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins
{
    [Info("TimeVote", "ThermalCube", 0.1)]
    [Description("A plugin to show the current time and start votes on a daytime change.")]
    class TimeVote
    {

        Oxide.Game.Rust.Libraries.Rust rust = new Oxide.Game.Rust.Libraries.Rust();

        [ChatCommand("time")]
        void TimeCommand(BasePlayer ply, string cmd, string[] args)
        {
            var sky = new TOD_Sky();
            sky.Cycle.Hour = 6.0f;
        }

    }
}
