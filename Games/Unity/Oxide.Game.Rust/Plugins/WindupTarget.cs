using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("WindupTarget", "ThermalCube", 0.1)]
    [Description("A plugin to automaticly reopen Practice-targets")]
    class WindupTarget : RustPlugin
    {

        void OnPluginLoaded(Plugin name)
        {
            Puts("Plugin '" + name + "' has been loaded");
        }
        
        void OnServerInitialized()
        {
            List<ReactiveTarget> targets = UnityEngine.Object.FindObjectsOfType<ReactiveTarget>().ToList();

            targets.ForEach(t =>
            {
                t.health = t.MaxHealth();
                t.SendNetworkUpdate();
                t.SetFlag(BaseEntity.Flags.On, true);
            });

        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            try
            {
                if (!(entity is ReactiveTarget) && !(hitinfo.Initiator is BasePlayer)) return;
                ReactiveTarget target = (ReactiveTarget)entity;
                BasePlayer ply = (BasePlayer)hitinfo.Initiator;
                if (target == null || ply == null) return;

                if (target.IsKnockedDown())
                {
                    timer.Once(10.0f, () => {

                        target.health = target.MaxHealth();
                        target.SendNetworkUpdate();
                        target.SetFlag(BaseEntity.Flags.On, true);

                    });
                }

            }catch(Exception ex)
            {
                PrintError(ex.Message);
            }
        }
        
    }
}
