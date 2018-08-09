using System;
using Smod2;
using Smod2.Config;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;

namespace SanyaPlugin
{
    class SpawnChecker : IEventHandlerSpawn
    {
        private Plugin plugin;

        public SpawnChecker(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnSpawn(PlayerSpawnEvent ev)
        {
            plugin.Debug("OnSpawn " + ev.Player.Name + ":" + ev.Player.TeamRole.Name + " x:" + ev.SpawnPos.x + " y:" + ev.SpawnPos.y + " z:" + ev.SpawnPos.z);

            if (EscapeHandler.isEscaper)
            {
                if (EscapeHandler.isDoubleSpawn)
                {
                    plugin.Debug("isDoubleSpawn : " + EscapeHandler.isDoubleSpawn);
                    EscapeHandler.isDoubleSpawn = false;
                }
                else
                {
                    plugin.Debug("isEscaper:" + EscapeHandler.isEscaper);
                    if (EscapeHandler.escape_player_id == ev.Player.PlayerId)
                    {
                        if (this.plugin.GetConfigBool("sanya_escape_spawn") && ev.Player.TeamRole.Role != Role.CHAOS_INSUGENCY)
                        {
                            plugin.Debug("escaper_id:" + EscapeHandler.escape_player_id + " / spawn_id:" + ev.Player.PlayerId);
                            plugin.Info("[Escape Spawner] Escape Successfully [" + ev.Player.Name + ":" + ev.Player.TeamRole.Role.ToString() + "]");
                            ev.SpawnPos = EscapeHandler.escape_pos;
                            EscapeHandler.isEscaper = false;
                        }
                        else
                        {
                            plugin.Info("[Escape Spawner] Disabled (config or CHAOS) [" + ev.Player.Name + ":" + ev.Player.TeamRole.Role.ToString() + "]");
                            EscapeHandler.isEscaper = false;
                            EscapeHandler.isDoubleSpawn = false;
                        }
                    }
                }

            }


        }
    }
}
