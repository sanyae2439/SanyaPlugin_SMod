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
    class EndEventer : IEventHandlerRoundEnd
    {
        private Plugin plugin;

        public EndEventer(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            plugin.Warn("Round End Status: " + ev.Status.ToString() + " Time:" + ev.Round.Duration);
        }
    }
}