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
    class Player106Lure : IEventHandlerLure
    {
        private Plugin plugin;

        public Player106Lure(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnLure(PlayerLureEvent ev)
        {
            ev.AllowContain = false;
            plugin.Info("OnLure : " + ev.AllowContain);
        }
    }
}
