using System;
using Smod2;
using Smod2.Config;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Commands;

namespace SanyaPlugin
{
    class WarheadActivator : IEventHandlerWarheadStartCountdown
    {
        private Plugin plugin;

        public WarheadActivator(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnStartCountdown(WarheadStartEvent ev)
        {
            plugin.Info("Warhead Started! By [" + ev.Activator.Name + "]Timeleft:" + ev.TimeLeft);
            ev.Activator.SetRank("red", "WarHead Activator");
        }
    }
}
