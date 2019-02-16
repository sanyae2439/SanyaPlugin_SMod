using System.Collections.Generic;
using Smod2;
using Smod2.API;
using Smod2.Attributes;

namespace SanyaPlugin
{
    [PluginDetails(
    author = "sanyae2439",
    name = "SanyaPlugin",
    description = "nya",
    id = "sanyae2439.sanyaplugin",
    version = "12.2.2",
    SmodMajor = 3,
    SmodMinor = 3,
    SmodRevision = 0
    )]

    class SanyaPlugin : Plugin

    {
        //システム系
        public string info_sender_to_ip;
        public int info_sender_to_port;
        public int spectator_slot;
        public bool night_mode;
        public bool title_timer;
        public bool cassie_subtitle;
        public bool friendly_warn;
        //SCP系
        public bool generators_fix;
        public bool scp914_changing;
        public bool scp106_portal_to_human;
        public int scp106_lure_speaktime;
        public bool scp106_cleanup;
        public bool infect_by_scp049_2;
        public int infect_limit_time;
        //人間系
        public bool handcuffed_cantopen;
        public bool radio_enhance;
        //独自要素
        public bool escape_spawn;
        public bool intercom_information;
        public int traitor_limitter;
        public int traitor_chance_percent;
        public int classd_startitem_percent;
        public ItemType classd_startitem_ok_itemid;
        public ItemType classd_startitem_no_itemid;
        public ItemType doorlock_itemid;
        public int doorlock_locked_second;
        public int doorlock_interval_second;
        //ダメージ系
        public float fallen_limit;
        public float usp_damage_multiplier_human;
        public float usp_damage_multiplier_scp;
        public float damage_divisor_scp173;
        public float damage_divisor_scp106;
        public float damage_divisor_scp049;
        public float damage_divisor_scp049_2;
        public float damage_divisor_scp096;
        public float damage_divisor_scp939;
        //回復
        public int recovery_amount_scp173;
        public int recovery_amount_scp106;
        public int recovery_amount_scp049;
        public int recovery_amount_scp049_2;
        public int recovery_amount_scp096;
        public int recovery_amount_scp939;
        //DefaultAmmo
        public int[] default_ammo_classd;
        public int[] default_ammo_scientist;
        public int[] default_ammo_guard;
        public int[] default_ammo_ci;
        public int[] default_ammo_ntfscientist;
        public int[] default_ammo_cadet;
        public int[] default_ammo_lieutenant;
        public int[] default_ammo_commander;

        public override void OnDisable()
        {
            Info("さにゃぷらぐいん Disabled");
        }

        public override void OnEnable()
        {
            Info("さにゃぷらぐいん Loaded [Ver" + this.Details.version + "]");
            Info("ずりねこ");
        }

        public override void Register()
        {
            //システム系
            AddConfig(new Smod2.Config.ConfigSetting("sanya_info_sender_to_ip", "hatsunemiku24.ddo.jp", Smod2.Config.SettingType.STRING, true, "サーバー情報送信先IP"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_info_sender_to_port", 37813, Smod2.Config.SettingType.NUMERIC, true, "サーバー情報送信先ポート"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_spectator_slot", 0, Smod2.Config.SettingType.NUMERIC, true, "プレイヤーリミット数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_night_mode", false, Smod2.Config.SettingType.BOOL, true, "NightModeを有効にする"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_title_timer", false, Smod2.Config.SettingType.BOOL, true, "プレイヤーリストタイトルにタイマー追加"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_cassie_subtitle", false, Smod2.Config.SettingType.BOOL, true, "放送に日英で字幕を付ける"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_friendly_warn", false, Smod2.Config.SettingType.BOOL, true, "FF時に警告を日英で表示"));
            //SCP系
            AddConfig(new Smod2.Config.ConfigSetting("sanya_generators_fix", false, Smod2.Config.SettingType.BOOL, true, "発電機の挙動を少し変更"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_scp914_changing", false, Smod2.Config.SettingType.BOOL, true, "SCP-914にプレイヤーが入った際の挙動を少し変更"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_scp106_portal_to_human", false, Smod2.Config.SettingType.BOOL, true, "SCP-106のポータルを生存者の足元に作成する機能"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_scp106_lure_speaktime", -1, Smod2.Config.SettingType.NUMERIC, true, "SCP-106の囮コンテナに入った際の放送できる時間"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_scp106_cleanup", false, Smod2.Config.SettingType.BOOL, true, "scp106_cleanupがバグった際に使う用"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_infect_by_scp049_2", false, Smod2.Config.SettingType.BOOL, true, "SCP-049-2がキルした際もSCP-049が治療可能に"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_infect_limit_time", 4, Smod2.Config.SettingType.NUMERIC, true, "SCP-049が治療できなくなるまでの時間"));
            //人間系
            AddConfig(new Smod2.Config.ConfigSetting("sanya_handcuffed_cantopen", false, Smod2.Config.SettingType.BOOL, true, "被拘束時にドア/エレベーターを操作不能に"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_radio_enhance", false, Smod2.Config.SettingType.BOOL, true, "レンジURで放送可能に"));
            //独自要素
            AddConfig(new Smod2.Config.ConfigSetting("sanya_escape_spawn", false, Smod2.Config.SettingType.BOOL, true, "NTFになる際の脱出地点を変更"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_intercom_information", false, Smod2.Config.SettingType.BOOL, true, "放送室のモニターに情報表示"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_traitor_limitter", -1, Smod2.Config.SettingType.NUMERIC, true, "NTF/CIが被拘束で脱出地点に行くと敵対陣営になれるための残存味方勢力リミット"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_traitor_chance_percent", 50, Smod2.Config.SettingType.NUMERIC, true, "敵対陣営になるチャレンジの成功率"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_classd_startitem_percent", -1, Smod2.Config.SettingType.NUMERIC, true, "クラスDスポーン地点にアイテムを設置（OKになる確率)"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_classd_startitem_ok_itemid", 0, Smod2.Config.SettingType.NUMERIC, true, "OKの場合に設置するアイテム"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_classd_startitem_no_itemid", -1, Smod2.Config.SettingType.NUMERIC, true, "NGの場合に設置するアイテム"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_doorlock_itemid", -1, Smod2.Config.SettingType.NUMERIC, true, "ドアロックできるアイテム"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_doorlock_locked_second", 10, Smod2.Config.SettingType.NUMERIC, true, "ドアロックできる時間"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_doorlock_interval_second", 60, Smod2.Config.SettingType.NUMERIC, true, "ドアロックのクールタイム"));
            //ダメージ調整系
            AddConfig(new Smod2.Config.ConfigSetting("sanya_fallen_limit", 10.0f, Smod2.Config.SettingType.FLOAT, true, "指定以下のダメージを無効にする"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_usp_damage_multiplier_human", 2.5f, Smod2.Config.SettingType.FLOAT, true, "USPの対人間ダメージ乗算値（調整しないと弱いので）"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_usp_damage_multiplier_scp", 5.0f, Smod2.Config.SettingType.FLOAT, true, "USPの対SCPダメージ乗算値（調整しないと弱いので）"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_damage_divisor_scp173", 1.0f, Smod2.Config.SettingType.FLOAT, true, "SCP-173が受けるダメージの減算値"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_damage_divisor_scp106", 1.0f, Smod2.Config.SettingType.FLOAT, true, "SCP-106が受けるダメージの減算値"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_damage_divisor_scp049", 1.0f, Smod2.Config.SettingType.FLOAT, true, "SCP-049が受けるダメージの減算値"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_damage_divisor_scp049_2", 1.0f, Smod2.Config.SettingType.FLOAT, true, "SCP-049-2が受けるダメージの減算値"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_damage_divisor_scp096", 1.0f, Smod2.Config.SettingType.FLOAT, true, "SCP-096が受けるダメージの減算値"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_damage_divisor_scp939", 1.0f, Smod2.Config.SettingType.FLOAT, true, "SCP-939が受けるダメージの減算値"));
            //回復系
            AddConfig(new Smod2.Config.ConfigSetting("sanya_recovery_amount_scp173", -1, Smod2.Config.SettingType.NUMERIC, true, "SCP-173が回復する量(キル時)"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_recovery_amount_scp106", -1, Smod2.Config.SettingType.NUMERIC, true, "SCP-106が回復する量(ディメンション脱出失敗時)"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_recovery_amount_scp049", -1, Smod2.Config.SettingType.NUMERIC, true, "SCP-049が回復する量(治療成功時)"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_recovery_amount_scp049_2", -1, Smod2.Config.SettingType.NUMERIC, true, "SCP-049-2回復する量(キル時)"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_recovery_amount_scp096", -1, Smod2.Config.SettingType.NUMERIC, true, "SCP-096が回復する量(キル時)"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_recovery_amount_scp939", -1, Smod2.Config.SettingType.NUMERIC, true, "SCP-939が回復する量(キル時)"));
            //default_ammo
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_classd",new int[] {15, 15, 15} ,Smod2.Config.SettingType.NUMERIC_LIST, true, "Dクラスの初期所持弾数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_scientist", new int[] { 20, 15, 15 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "科学者の初期所持弾数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_guard", new int[] { 0, 35, 0 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "Facility Guardの初期所持弾数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_ci", new int[] { 0, 200, 20 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "カオスの初期所持弾数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_ntfscientist", new int[] { 80, 40, 40 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "NTF Scientistの初期所持弾数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_cadet", new int[] { 10, 10, 80 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "NTF Cadetの初期所持弾数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_lieutenant", new int[] { 80, 40, 40 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "NTF Lieutenantの初期所持弾数"));
            AddConfig(new Smod2.Config.ConfigSetting("sanya_default_ammo_commander", new int[] { 130, 50, 50 }, Smod2.Config.SettingType.NUMERIC_LIST, true, "NTF Commanderの初期所持弾数"));

            //Add
            AddCommand("sanya", new CommandHandler(this));
            AddEventHandlers(new EventHandler(this),Smod2.Events.Priority.Highest);
        }

        public void ReloadConfig()
        {
            info_sender_to_ip = GetConfigString("sanya_info_sender_to_ip");
            info_sender_to_port = GetConfigInt("sanya_info_sender_to_port");
            spectator_slot = GetConfigInt("sanya_spectator_slot");
            night_mode = GetConfigBool("sanya_night_mode");
            title_timer = GetConfigBool("sanya_title_timer");
            cassie_subtitle = GetConfigBool("sanya_cassie_subtitle");
            friendly_warn = GetConfigBool("sanya_friendly_warn");

            generators_fix = GetConfigBool("sanya_generators_fix");
            scp914_changing = GetConfigBool("sanya_scp914_changing");
            scp106_portal_to_human = GetConfigBool("sanya_scp106_portal_to_human");
            scp106_lure_speaktime = GetConfigInt("sanya_scp106_lure_speaktime");
            scp106_cleanup = GetConfigBool("sanya_scp106_cleanup");
            infect_by_scp049_2 = GetConfigBool("sanya_infect_by_scp049_2");
            infect_limit_time = GetConfigInt("sanya_infect_limit_time");

            handcuffed_cantopen = GetConfigBool("sanya_handcuffed_cantopen");
            radio_enhance = GetConfigBool("sanya_radio_enhance");

            escape_spawn = GetConfigBool("sanya_escape_spawn");
            intercom_information = GetConfigBool("sanya_intercom_information");
            traitor_limitter = GetConfigInt("sanya_traitor_limitter");
            traitor_chance_percent = GetConfigInt("sanya_traitor_chance_percent");
            classd_startitem_percent = GetConfigInt("sanya_classd_startitem_percent");
            classd_startitem_ok_itemid = (ItemType)GetConfigInt("sanya_classd_startitem_ok_itemid");
            classd_startitem_no_itemid = (ItemType)GetConfigInt("sanya_classd_startitem_no_itemid");
            doorlock_itemid = (ItemType)GetConfigInt("sanya_doorlock_itemid");
            doorlock_locked_second = GetConfigInt("sanya_doorlock_locked_second");
            doorlock_interval_second = GetConfigInt("sanya_doorlock_interval_second");

            fallen_limit = GetConfigFloat("sanya_fallen_limit");
            usp_damage_multiplier_human = GetConfigFloat("sanya_usp_damage_multiplier_human");
            usp_damage_multiplier_scp = GetConfigFloat("sanya_usp_damage_multiplier_scp");
            damage_divisor_scp173 = GetConfigFloat("sanya_damage_divisor_scp173");
            damage_divisor_scp106 = GetConfigFloat("sanya_damage_divisor_scp106");
            damage_divisor_scp049 = GetConfigFloat("sanya_damage_divisor_scp049");
            damage_divisor_scp049_2 = GetConfigFloat("sanya_damage_divisor_scp049_2");
            damage_divisor_scp096 = GetConfigFloat("sanya_damage_divisor_scp096");
            damage_divisor_scp939 = GetConfigFloat("sanya_damage_divisor_scp939");

            recovery_amount_scp173 = GetConfigInt("sanya_recovery_amount_scp173");
            recovery_amount_scp106 = GetConfigInt("sanya_recovery_amount_scp106");
            recovery_amount_scp049 = GetConfigInt("sanya_recovery_amount_scp049");
            recovery_amount_scp049_2 = GetConfigInt("sanya_recovery_amount_scp049_2");
            recovery_amount_scp096 = GetConfigInt("sanya_recovery_amount_scp096");
            recovery_amount_scp939 = GetConfigInt("sanya_recovery_amount_scp939");

            default_ammo_classd = GetConfigIntList("sanya_default_ammo_classd");
            default_ammo_scientist = GetConfigIntList("sanya_default_ammo_scientist");
            default_ammo_guard = GetConfigIntList("sanya_default_ammo_guard");
            default_ammo_ci = GetConfigIntList("sanya_default_ammo_ci");
            default_ammo_cadet = GetConfigIntList("sanya_default_ammo_cadet");
            default_ammo_lieutenant = GetConfigIntList("sanya_default_ammo_lieutenant");
            default_ammo_commander = GetConfigIntList("sanya_default_ammo_commander");
            default_ammo_ntfscientist = GetConfigIntList("sanya_default_ammo_ntfscientist");
        }
    }
}
