using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Config;

namespace SanyaPlugin
{
    class PocketDimensionCleaner : IEventHandlerPocketDimensionDie
    {
        private Plugin plugin;
        private int temphealth;

        public PocketDimensionCleaner(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnPocketDimensionDie(PlayerPocketDimensionDieEvent ev)
        {
            if(ev.Die && this.plugin.GetConfigBool("sanya_pocket_cleanup"))
            {
                plugin.Info("[Pocket Cleaner] Cleaning Start... (" + ev.Player.Name + ")");

                temphealth = ev.Player.GetHealth();
                ev.Player.Damage(1, DamageType.POCKET);
                if(temphealth == ev.Player.GetHealth())
                {
                    plugin.Info("[Pocket Cleaner] Protection (" + ev.Player.Name + ")");
                }
                else
                {
                    plugin.Info("[Pocket Cleaner] Cleaning Complete (" + ev.Player.Name + ")");
                    ev.Player.Teleport(new Vector(0, 0, 0));
                    ev.Player.Kill(DamageType.POCKET);
                }

            }

        }
    }
}
