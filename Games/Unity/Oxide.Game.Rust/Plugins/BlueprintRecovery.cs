using Oxide.Core.Plugins;
using System;

namespace Oxide.Plugins
{
    [Info("BlueprintRecovery", "ThermalCube & Killer", 0.1)]
    [Description("A plugin to recover blueprint fragments from blueprints")]
    class BlueprintRecovery : RustPlugin
    {

        void OnPluginLoaded(Plugin name)
        {
            Puts("Plugin '" + name + "' has been loaded");
        }
        
        [ChatCommand("recover")]
        void recover(BasePlayer ply, string cmd, string[] args)
        {

            Item holding = ply.GetActiveItem();

            if (!holding.IsBlueprint())
            {
                rust.SendChatMessage(ply, "[BP-Recovery]", "Du must ein Blueprint halten um diesen Befehl nutzen zu können!");
                return;
            }
            Item item = holding;
            item.SetFlag(Item.Flag.Blueprint, false);
            ResearchTable rt = new ResearchTable();
            int frags = (int)Math.Ceiling(Math.Min(Math.Min(rt.BPFragsForFull(item), 200), 1500)*0.75);
            if (item.info.itemid == 649603450){ frags *= 2; }

            ply.GetActiveItem().Remove(0.0f);
            ply.GiveItem(ItemManager.CreateByItemID(1351589500, frags));
            rust.SendChatMessage(ply, "[BP-Recovery]", String.Format("Du hast <color=#008080ff>{0}</color> Blueprint-Fragments erhalten!", frags));

        }
        
    }
}
