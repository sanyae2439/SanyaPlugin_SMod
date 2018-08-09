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
    class NukeDontLock : IEventHandlerRoundStart
    {
        private Plugin plugin;

        public NukeDontLock(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            if (this.plugin.GetConfigBool("sanya_warhead_dontlock"))
            {
                foreach (Door door in ev.Server.Map.GetDoors())
                {
                    door.DontOpenOnWarhead = true;
                }
                plugin.Info("NukeOpenDoors Stopped");
            }
        }
    }
}
