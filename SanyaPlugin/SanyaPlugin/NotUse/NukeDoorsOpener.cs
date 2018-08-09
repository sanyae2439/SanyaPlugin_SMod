using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Config;

namespace SanyaPlugin
{
    class NukeDoorsOpener : IEventHandlerWarheadStartCountdown
    {
        private Plugin plugin;

        public NukeDoorsOpener(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnStartCountdown(WarheadStartEvent ev)
        {
            plugin.Info("nukestart");
            if (this.plugin.GetConfigBool("sanya_warhead_forceopen"))
            {
                foreach (Door door in plugin.pluginManager.Server.Map.GetDoors())
                {
                    door.Open = true;
                }
                plugin.Info("alpha Warhead Force Doors Open!");
            }
        }
    }
}
