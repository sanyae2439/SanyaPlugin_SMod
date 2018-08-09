using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;

namespace SanyaPlugin
{
    class EscapeHandler : IEventHandlerCheckEscape
    {
        private Plugin plugin;
        private Vector escaper;
        private Role targetRole;

        public EscapeHandler(Plugin plugin) {
            this.plugin = plugin;
        }

        public void OnCheckEscape(PlayerCheckEscapeEvent ev)
        {
            plugin.Info("OnCheckEscape " + ev.Player.Name + ":" + ev.Player.TeamRole.Role);

            ev.AllowEscape = false;

            escaper = ev.Player.GetPosition();

            switch (ev.Player.TeamRole.Role)
            {
                case Role.CLASSD:
                    {
                        if (ev.Player.IsHandcuffed()) {
                            targetRole = Role.NTF_CADET;
                        }
                        else
                        {
                            targetRole = Role.CHAOS_INSUGENCY;
                        }
                        
                        break;
                    }
                case Role.SCIENTIST:
                    {
                        if (ev.Player.IsHandcuffed())
                        {
                            targetRole = Role.CHAOS_INSUGENCY;
                        }
                        else
                        {
                            targetRole = Role.NTF_SCIENTIST;
                        }

                        break;
                    }
            }

            ev.Player.ChangeRole(targetRole);

            ev.Player.Teleport(escaper);

            plugin.Info("Escape Success " + targetRole.ToString());
        }
    }
}
