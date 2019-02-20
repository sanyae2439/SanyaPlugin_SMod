using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Config;

namespace SanyaPlugin
{
    [PluginDetails(
    author = "sanyae2439",
    name = "SanyaPlugin",
    description = "nya",
    id = "sanyae2439.sanyaplugin",
    configPrefix = "sanya",
    version = "12.3",
    SmodMajor = 3,
    SmodMinor = 3,
    SmodRevision = 1
    )]

    class SanyaPlugin : Plugin
    {
        //test
        public bool nuketest = false;

        //システム系
        [ConfigOption] //サーバー情報送信先IP
        internal string info_sender_to_ip = "hatsunemiku24.ddo.jp";
        [ConfigOption] //サーバー情報送信先ポート
        internal int info_sender_to_port = 37813;
        [ConfigOption] //プレイヤーリミット数
        internal int spectator_slot = 0;
        [ConfigOption] //NightModeを有効にする
        internal bool night_mode = false;
        [ConfigOption] //プレイヤーリストタイトルにタイマー追加
        internal bool title_timer = false;
        [ConfigOption] //放送に日英で字幕を付ける
        internal bool cassie_subtitle = false;
        [ConfigOption] //FF時に警告を日英で表示
        internal bool friendly_warn = false;
        [ConfigOption] //リザルト無しでラウンドを終了させる
        internal bool summary_less_mode = false;
        [ConfigOption] //ラウンド終了時に全員をSPECTATORにする
        internal bool endround_all_spectator = false;

        //SCP系
        [ConfigOption] //発電機の挙動を少し変更
        internal bool generators_fix = false;
        [ConfigOption] //SCP-914にプレイヤーが入った際の挙動を少し変更
        internal bool scp914_changing = false;
        [ConfigOption] //SCP-106のポータルを生存者の足元に作成する機能
        internal bool scp106_portal_to_human = false;
        [ConfigOption] //ポータル機能の最初のクールタイム
        internal int scp106_portal_to_human_wait = 180;
        [ConfigOption] //SCP-106の囮コンテナに入った際の放送できる時間
        internal int scp106_lure_speaktime = -1;
        [ConfigOption] //scp106_cleanupがバグった際に使う用
        internal bool scp106_cleanup = false;
        [ConfigOption] //SCP-049-2がキルした際もSCP-049が治療可能に
        internal bool infect_by_scp049_2 = false;
        [ConfigOption] //SCP-049が治療できなくなるまでの時間
        internal int infect_limit_time = 4;

        //人間系
        [ConfigOption] //被拘束時にドア/エレベーターを操作不能に
        internal bool handcuffed_cantopen = false;
        [ConfigOption] //レンジURで放送可能に
        internal bool radio_enhance = false;

        //独自要素
        [ConfigOption] //独自自動核
        internal bool original_auto_nuke = false;
        [ConfigOption] //NTFになる際の脱出地点を変更
        internal bool escape_spawn = false;
        [ConfigOption] //放送室のモニターに情報表示
        internal bool intercom_information = false;
        [ConfigOption] //NTF/CIが被拘束で脱出地点に行くと敵対陣営になれるための残存味方勢力リミット
        internal int traitor_limitter = -1;
        [ConfigOption] //敵対陣営になるチャレンジの成功率
        internal int traitor_chance_percent = 50;
        [ConfigOption] //クラスDスポーン地点にアイテムを設置（OKになる確率)
        internal int classd_startitem_percent = -1;
        [ConfigOption] //OKの場合に設置するアイテム
        internal int classd_startitem_ok_itemid = 0;
        [ConfigOption] //NGの場合に設置するアイテム
        internal int classd_startitem_no_itemid = -1;
        [ConfigOption] //ドアロックできるアイテム
        internal int doorlock_itemid = -1;
        [ConfigOption] //ドアロックできる時間
        internal int doorlock_locked_second = 10;
        [ConfigOption] //ドアロックのクールタイム
        internal int doorlock_interval_second = 60;

        //ダメージ系
        [ConfigOption] //指定以下のダメージを無効にする
        internal float fallen_limit = 10.0f;
        [ConfigOption] //USPの対人間ダメージ乗算値（調整しないと弱いので）
        internal float usp_damage_multiplier_human = 2.5f;
        [ConfigOption] //USPの対SCPダメージ乗算値（調整しないと弱いので）
        internal float usp_damage_multiplier_scp = 5.0f;
        [ConfigOption] //SCP-173が受けるダメージの減算値
        internal float damage_divisor_scp173 = 1.0f;
        [ConfigOption] //SCP-106が受けるダメージの減算値
        internal float damage_divisor_scp106 = 1.0f;
        [ConfigOption] //SCP-049が受けるダメージの減算値
        internal float damage_divisor_scp049 = 1.0f;
        [ConfigOption] //SCP-049-2が受けるダメージの減算値
        internal float damage_divisor_scp049_2 = 1.0f;
        [ConfigOption] //SCP-096が受けるダメージの減算値
        internal float damage_divisor_scp096 = 1.0f;
        [ConfigOption] //SCP-939が受けるダメージの減算値
        internal float damage_divisor_scp939 = 1.0f;

        //回復
        [ConfigOption] //SCP-173が回復する量(キル時)
        internal int recovery_amount_scp173 = -1;
        [ConfigOption] //SCP-106が回復する量(ディメンション脱出失敗時)
        internal int recovery_amount_scp106 = -1;
        [ConfigOption] //SCP-049が回復する量(治療成功時)
        internal int recovery_amount_scp049 = -1;
        [ConfigOption] //SCP-049-2が回復する量(キル時)
        internal int recovery_amount_scp049_2 = -1;
        [ConfigOption] //SCP-096が回復する量(キル時)
        internal int recovery_amount_scp096 = -1;
        [ConfigOption] //SCP-939が回復する量(キル時)
        internal int recovery_amount_scp939 = -1;

        //DefaultAmmo
        [ConfigOption] //Dクラスの初期所持弾数
        internal int[] default_ammo_classd = new int[] { 15, 15, 15 };
        [ConfigOption] //科学者の初期所持弾数
        internal int[] default_ammo_scientist = new int[] { 20, 15, 15 };
        [ConfigOption] //Facility Guardの初期所持弾数
        internal int[] default_ammo_guard = new int[] { 0, 35, 0 };
        [ConfigOption] //カオスの初期所持弾数
        internal int[] default_ammo_ci = new int[] { 0, 200, 20 };
        [ConfigOption] //NTF Scientistの初期所持弾数
        internal int[] default_ammo_ntfscientist = new int[] { 80, 40, 40 };
        [ConfigOption] //NTF Cadetの初期所持弾数
        internal int[] default_ammo_cadet = new int[] { 10, 10, 80 };
        [ConfigOption] //NTF Lieutenantの初期所持弾数
        internal int[] default_ammo_lieutenant = new int[] { 80, 40, 40 };
        [ConfigOption] //NTF Commanderの初期所持弾数
        internal int[] default_ammo_commander = new int[] { 130, 50, 50 };

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
            classd_startitem_ok_itemid = GetConfigInt("sanya_classd_startitem_ok_itemid");
            classd_startitem_no_itemid = GetConfigInt("sanya_classd_startitem_no_itemid");
            doorlock_itemid = GetConfigInt("sanya_doorlock_itemid");
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
