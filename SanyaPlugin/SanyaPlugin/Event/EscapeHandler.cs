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
        public static bool isEscaper = false;
        public static bool isDoubleSpawn = false;
        public static Vector escape_pos;
        public static int escape_player_id;

        public EscapeHandler(Plugin plugin) {
            this.plugin = plugin;
        }

        public void OnCheckEscape(PlayerCheckEscapeEvent ev)
        {
            plugin.Debug("OnCheckEscape " + ev.Player.Name + ":" + ev.Player.TeamRole.Role);

            isEscaper = true;
            isDoubleSpawn = true;
            escape_player_id = ev.Player.PlayerId;
            escape_pos = ev.Player.GetPosition();

            plugin.Debug("escaper x:" + escape_pos.x + " y:" + escape_pos.y + " z:" + escape_pos.z);
        }
    }
}
