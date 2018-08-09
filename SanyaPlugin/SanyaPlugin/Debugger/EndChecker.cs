using System;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Config;
using Smod2.Commands;

namespace SanyaPlugin
{
    class EndChecker : IEventHandlerCheckRoundEnd
    {
        private Plugin plugin;
        private static ROUND_END_STATUS tempstatus;

        public EndChecker(Plugin plugin)
        {
            this.plugin = plugin;
            tempstatus = ROUND_END_STATUS.OTHER_VICTORY;
        }

        public void OnCheckRoundEnd(CheckRoundEndEvent ev)
        {
            if(tempstatus != ev.Status)
            {
                tempstatus = ev.Status;
                plugin.Warn("EndStatus Changed : " + ev.Status.ToString());
                if(tempstatus == ROUND_END_STATUS.ON_GOING)
                {
                    plugin.Warn("Round Started : " + tempstatus.ToString());
                }else{
                    ev.Status = ROUND_END_STATUS.FORCE_END;
                    ev.Round.EndRound();
                    plugin.Warn("Round Force Ended : " + tempstatus.ToString());
                    plugin.Warn("Class-D:" + ev.Round.Stats.ClassDAlive + " Scientist:" + ev.Round.Stats.ScientistsAlive + " NTF:" + ev.Round.Stats.NTFAlive + " SCP:" + ev.Round.Stats.SCPAlive);
                }
            }
        }
    }
}
