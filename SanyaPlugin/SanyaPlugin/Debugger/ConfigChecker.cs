using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Config;

namespace SanyaPlugin
{
    class ConfigChecker : IEventHandlerRoundStart
    {
        private Plugin plugin;

        public ConfigChecker(Plugin plugin)
        {
            this.plugin = plugin;
        }


        public void OnRoundStart(RoundStartEvent ev)
        {
            plugin.Debug("RoundStart!");
            plugin.Debug("sanya_escape_spawn :" + plugin.GetConfigBool("sanya_escape_spawn"));
            plugin.Debug("sanya_infect_by_scp049_2 :" + plugin.GetConfigBool("sanya_infect_by_scp049_2"));
            plugin.Debug("sanya_infect_limit_time :" + plugin.GetConfigInt("sanya_infect_limit_time"));
            plugin.Debug("sanya_warhead_dontlock :" + plugin.GetConfigBool("sanya_warhead_dontlock"));
            //plugin.Debug("sanya_warhead_forceopen :" + plugin.GetConfigBool("sanya_warhead_forceopen"));

            plugin.Debug("sanya_scp173_duplicate :" + plugin.GetConfigBool("sanya_scp173_duplicate"));
            plugin.Debug("sanya_scp173_duplicate_hp :" + plugin.GetConfigInt("sanya_scp173_duplicate_hp"));
            plugin.Debug("sanya_scp049_duplicate :" + plugin.GetConfigBool("sanya_scp049_duplicate"));
            plugin.Debug("sanya_scp049_duplicate_hp :" + plugin.GetConfigInt("sanya_scp049_duplicate_hp"));
            plugin.Debug("sanya_scp939_duplicate :" + plugin.GetConfigBool("sanya_scp939_duplicate"));
            plugin.Debug("sanya_scp939_duplicate_hp :" + plugin.GetConfigInt("sanya_scp939_duplicate_hp"));
            plugin.Debug("sanya_scp049_2_duplicate :" + plugin.GetConfigBool("sanya_scp049_2_duplicate"));
            plugin.Debug("sanya_scp049_2_duplicate_hp :" + plugin.GetConfigInt("sanya_scp049_2_duplicate_hp"));
            plugin.Debug("sanya_scp106_duplicate :" + plugin.GetConfigBool("sanya_scp106_duplicate"));
            plugin.Debug("sanya_scp106_duplicate_hp :" + plugin.GetConfigInt("sanya_scp106_duplicate_hp"));
        }
    }
}
