using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Config;

namespace SanyaPlugin
{
    class HurtChanger : IEventHandlerPlayerHurt
    {
        private Plugin plugin;
        private Vector lastpos;
        private Player targetplayer;

        public HurtChanger(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            targetplayer = ev.Attacker;

            //----------------------------------------------------オールドマン複製------------------------------------------------
                if (ev.DamageType == DamageType.SCP_106 && this.plugin.GetConfigBool("sanya_scp106_duplicate"))
            {
                plugin.Info("[Duplicator] 106 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
                ev.Damage = 0.0f;
                lastpos = ev.Player.GetPosition();
                ev.Player.ChangeRole(Role.SCP_106, true, false);
                ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp106_duplicate_hp"));
                ev.Player.Teleport(lastpos);
            }
            //----------------------------------------------------オールドマン複製------------------------------------------------

            if ( (ev.Player.GetHealth() - ev.Damage) < 0) //HP0になる被弾時
            {
                //----------------------------------------------------ペスト治療------------------------------------------------
                if (ev.DamageType == DamageType.SCP_049_2 && this.plugin.GetConfigBool("sanya_infect_by_scp049_2"))
                {
                    plugin.Info("[Infector] 049-2 Infected!(Limit:" + this.plugin.GetConfigInt("sanya_infect_limit_time") + "s) [" + targetplayer.Name + "->" + ev.Player.Name + "]");
                    ev.DamageType = DamageType.SCP_049;
                    ev.Player.Infect(this.plugin.GetConfigInt("sanya_infect_limit_time"));
                }
                //----------------------------------------------------ペスト治療------------------------------------------------

                //----------------------------------------------------複製-------------------------------------------------
                if (ev.DamageType == DamageType.SCP_049_2 && this.plugin.GetConfigBool("sanya_scp049_2_duplicate"))
                {
                    plugin.Info("[Duplicator] 049-2 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
                    ev.Damage = 0.0f;
                    lastpos = ev.Player.GetPosition();
                    ev.Player.ChangeRole(Role.SCP_049_2, true, false);
                    ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp049_2_duplicate_hp"));
                    ev.Player.Teleport(lastpos);
                }

                if ( (ev.DamageType == DamageType.SCP_939) && this.plugin.GetConfigBool("sanya_scp939_duplicate"))
                {
                    plugin.Info("[Duplicator] 939 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
                    ev.Damage = 0.0f;
                    lastpos = ev.Player.GetPosition();
                    ev.Player.ChangeRole(Role.SCP_939_89, true, false);
                    ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp939_duplicate_hp"));
                    ev.Player.Teleport(lastpos);
                }

                if (ev.DamageType == DamageType.SCP_049 && this.plugin.GetConfigBool("sanya_scp049_duplicate"))
                {
                    plugin.Info("[Duplicator] 049 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
                    ev.Damage = 0.0f;
                    lastpos = ev.Player.GetPosition();
                    ev.Player.ChangeRole(Role.SCP_049, true, false);
                    ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp049_duplicate_hp"));
                    ev.Player.Teleport(lastpos);
                }

                if (ev.DamageType == DamageType.SCP_173 && this.plugin.GetConfigBool("sanya_scp173_duplicate"))
                {
                    plugin.Info("[Duplicator] 173 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
                    ev.Damage = 0.0f;
                    lastpos = ev.Player.GetPosition();
                    ev.Player.ChangeRole(Role.SCP_173, true, false);
                    ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp173_duplicate_hp"));
                    ev.Player.Teleport(lastpos);
                }
                //----------------------------------------------------複製-------------------------------------------------
            }
        }
    }
}