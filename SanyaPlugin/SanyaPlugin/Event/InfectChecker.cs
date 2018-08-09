using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Config;

namespace SanyaPlugin
{
    class InfectChecker : IEventHandlerInfected
    {
        private Plugin plugin;

        public InfectChecker(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnPlayerInfected(PlayerInfectedEvent ev)
        {
            plugin.Info("[Infector] 049 Infected!(Limit:" + this.plugin.GetConfigInt("sanya_infect_limit_time") + "s) [" + ev.Attacker.Name + "->" + ev.Player.Name + "]");
            ev.InfectTime = this.plugin.GetConfigInt("sanya_infect_limit_time");
        }
    }
}
