using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;

namespace SanyaPlugin
{
    class KillChanger : IEventHandlerPlayerDie
    {
        private Plugin plugin;
        private Vector lastpos;
        private Player targetplayer;

        public KillChanger(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            targetplayer = ev.Killer;

            if (ev.Player.TeamRole.Role == Role.SCP_049_2)
            {
                plugin.Info("049-2 Dead! (" + targetplayer.Name + "  -> " + ev.Player.Name + ")");
                lastpos = targetplayer.GetPosition();
                targetplayer.ChangeRole(Role.SCP_049_2, true, false);
                targetplayer.Teleport(lastpos);
            }
        }
    }
}
