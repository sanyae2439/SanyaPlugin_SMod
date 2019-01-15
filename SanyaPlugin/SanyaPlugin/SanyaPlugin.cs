using System.Collections.Generic;
using Smod2;
using Smod2.Attributes;

namespace SanyaPlugin
{
    [PluginDetails(
    author = "sanyae2439",
    name = "SanyaPlugin",
    description = "nya",
    id = "sanyae2439.sanyaplugin",
    version = "11.0",
    SmodMajor = 3,
    SmodMinor = 2,
    SmodRevision = 2
    )]

    class SanyaPlugin : Plugin

    {
        public override void OnDisable()
        {

        }

        public override void OnEnable()
        {
            this.Info("さにゃぷらぐいん Loaded [Ver" + this.Details.version + "]");
            this.Info("ずりにゃん");
        }

        public override void Register()
        {
            //InfoSender
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_info_sender_to", "hatsunemiku24.ddo.jp", Smod2.Config.SettingType.STRING, true, "sanya_info_sender_to"));
            
            //小物系
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_spectator_slot", 0, Smod2.Config.SettingType.NUMERIC, true, "sanya_spectator_slot"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_title_timer", true, Smod2.Config.SettingType.BOOL, true, "sanya_title_timer"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_cassie_subtitle", false, Smod2.Config.SettingType.BOOL, true, "sanya_cassie_subtitle"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_friendly_warn", false, Smod2.Config.SettingType.BOOL, true, "sanya_friendly_warn"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_scp914_changing", false, Smod2.Config.SettingType.BOOL, true, "sanya_scp914_changing"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_classd_startitem_percent", 20, Smod2.Config.SettingType.NUMERIC, true, "sanya_classd_startitem_percent"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_classd_startitem_ok_itemid", 0 , Smod2.Config.SettingType.NUMERIC, true, "sanya_classd_startitem_ok_itemid"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_classd_startitem_no_itemid", -1, Smod2.Config.SettingType.NUMERIC, true, "sanya_classd_startitem_no_itemid"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_door_lockable", false, Smod2.Config.SettingType.BOOL, true, "sanya_door_lockable"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_door_lockable_second", 10, Smod2.Config.SettingType.NUMERIC, true, "sanya_door_lockable_second"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_door_lockable_interval", 60, Smod2.Config.SettingType.NUMERIC, true, "sanya_door_lockable_interval"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_fallen_limit", 10, Smod2.Config.SettingType.NUMERIC, true, "sanya_fallen_limit"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_usp_damage_multiplier_human", 2.0f, Smod2.Config.SettingType.FLOAT, true, "sanya_usp_damage_multiplier_human"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_usp_damage_multiplier_scp", 5.0f, Smod2.Config.SettingType.FLOAT, true, "sanya_usp_damage_multiplier_scp"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_handcuffed_cantopen", true, Smod2.Config.SettingType.BOOL, true, "sanya_handcuffed_cantopen"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_radio_enhance", false, Smod2.Config.SettingType.BOOL, true, "sanya_radio_enhance"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_intercom_information", true, Smod2.Config.SettingType.BOOL, true, "sanya_intercom_information"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_escape_spawn", true, Smod2.Config.SettingType.BOOL, true, "sanya_escape_spawn"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_pocket_cleanup", false, Smod2.Config.SettingType.BOOL, true, "sanya_pocket_cleanup"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_infect_by_scp049_2", true, Smod2.Config.SettingType.BOOL, true, "sanya_infect"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_infect_limit_time", 4, Smod2.Config.SettingType.NUMERIC, true, "sanya_infect_limit_time"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_traitor_enabled", false, Smod2.Config.SettingType.BOOL, true, "sanya_traitor_enabled"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_traitor_limitter", 4, Smod2.Config.SettingType.NUMERIC, true, "sanya_traitor_limitter"));
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_traitor_chance_percent", 50, Smod2.Config.SettingType.NUMERIC, true, "sanya_traitor_chance_percent"));

            //SCPごとの回復値（新）
            var amountsdic = new Dictionary<string, string>()
            {
                {"SCP_173","-1" },
                {"SCP_106","-1" },
                {"SCP_049","-1" },
                {"SCP_049_2","-1" },
                {"SCP_096","-1" },
                {"SCP_939","-1" },
            };
            this.AddConfig(new Smod2.Config.ConfigSetting("sanya_scp_actrecovery_amounts", amountsdic, Smod2.Config.SettingType.DICTIONARY, true, "sanya_scp_actrecovery_amounts"));


            //EventHandler
            this.AddEventHandlers(new EventHandler(this));
        }
    }
}
