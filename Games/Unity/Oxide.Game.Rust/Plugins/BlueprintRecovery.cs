using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

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

            List<Item> blueprints = ply.inventory.containerBelt.itemList.FindAll(i => i.IsBlueprint());

            if (blueprints.Count == 0)
            {
                rust.SendChatMessage(ply, "[BP-Recovery]", "Du must mindestens ein Blueprint halten um diesen Befehl nutzen zu können!");
                return;
            }

            blueprints.ForEach(bp =>
            {

                Item item = bp;
                item.SetFlag(Item.Flag.Blueprint, false);
                ResearchTable rt = new ResearchTable();
                int frags = (int)Math.Ceiling(Math.Min(Math.Min(rt.BPFragsForFull(item), 200), 1500) * 0.75);
                if (item.info.itemid == 649603450) { frags *= 2; }

                ply.GetActiveItem().Remove(0.0f);
                ply.GiveItem(ItemManager.CreateByItemID(1351589500, frags));
                rust.SendChatMessage(ply, "[BP-Recovery]", String.Format("Du hast <color=#008080ff>{0}</color> Blueprint-Fragments erhalten!", frags));
                
            });

            
        }
        
    }
}
