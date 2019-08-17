using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using MEC;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Lang;
using Smod2.Config;
using UnityEngine;
using ServerMod2.API;

namespace SanyaPlugin
{
    [PluginDetails(
    author = "sanyae2439",
    name = "SanyaPlugin",
    description = "nya",
    id = "sanyae2439.sanyaplugin",
    configPrefix = "sanya",
    langFile = nameof(SanyaPlugin),
    version = "13.4.1",
    SmodMajor = 3,
    SmodMinor = 5,
    SmodRevision = 1
    )]
    public class SanyaPlugin : Plugin
    {
        //instance
        static internal SanyaPlugin plugin;

        //LayerMask
        public const int cctvmask = 262144;
        public const int doormask = 134234112;
        public const int playermask = 1208246273;
        public const int playerpluselevatormask = 1208246289;
        public const int teslamask = 4;
        public const int ragdollmask = 131072;

        //test
        static public bool test = false;

        //public
        static public System.DateTime roundStartTime;
        static public string scp_override_steamid = "";
        static public bool isAirBombGoing = false;

        //playersdata
        internal List<PlayerData> playersData;

        //システム系
        [ConfigOption] //サーバー情報送信先IP
        internal string info_sender_to_ip = "hatsunemiku24.ddo.jp";
        [ConfigOption] //サーバー情報送信先ポート
        internal int info_sender_to_port = 37813;
        [ConfigOption] //SteamがLimitedかどうかチェックする
        internal bool steam_kick_limited = false;
        [ConfigOption] //motdの有効化
        internal bool motd_enabled = false;
        [ConfigOption] //ログイン時メッセージ（特定role指定）
        internal string motd_target_role = "";
        [ConfigOption] //ゲームモードランダムの率
        internal int[] event_mode_weight = new int[] { 1, -1, -1, -1, -1, -1 };
        [ConfigOption] //停電時の発電機起動時間
        internal float night_generator_duration = 30;
        [ConfigOption] //反乱時のドロップ追加数
        internal int classd_ins_items = 10;
        [ConfigOption] //中層スタート時のガードの数
        internal int hczstart_mtf_and_ci = 3;
        [ConfigOption] //プレイヤーリストタイトルにタイマー追加
        internal bool title_timer = false;
        [ConfigOption] //放送に日英で字幕を付ける
        internal bool cassie_subtitle = false;
        [ConfigOption] //FF時に警告を日英で表示
        internal bool friendly_warn = false;
        [ConfigOption] //FFの被害者と加害者両方へ@キーコンソールへ表示（上と併用可能）
        internal bool friendly_warn_console = false;
        [ConfigOption] //ラウンド終了時に全員を無敵にする
        internal bool endround_all_godmode = false;
        [ConfigOption] //核起動開始時に一部を除きすべてのドアをオープンする
        internal bool nuke_start_countdown_door_lock = false;
        [ConfigOption] //カオスとSCPが同時にいてもラウンド終了しない
        internal bool ci_and_scp_noend = false;
        [ConfigOption] //最初の増援を早める倍率
        internal float first_respawn_time_fast = 1.0f;
        [ConfigOption] //ラウンド待ちのときにTutorialでスポーンさせる
        internal bool waiting_for_match_spawn = false;

        //Playersデータ&LevelEXP
        [ConfigOption] //playerDataを保存するか
        internal bool data_enabled = false;
        [ConfigOption] //Level機能の有効
        internal bool level_enabled = false;
        [ConfigOption] //Kill時のExp
        internal int level_exp_kill = 3;
        [ConfigOption] //Death時のExp
        internal int level_exp_death = 1;
        [ConfigOption] //勝利時のExp
        internal int level_exp_win = 10;
        [ConfigOption] //勝利以外のExp
        internal int level_exp_other = 3;

        //UserCommand
        [ConfigOption] //ユーザーコマンドの有効化
        internal bool user_command_enabled = false;
        [ConfigOption] //コマンドクールタイム（フレーム）
        internal int user_command_cooltime = 20;
        [ConfigOption] //コマンドの個別有効化（.kill）
        internal bool user_command_enabled_kill = true;
        [ConfigOption] //コマンドの個別有効化（.sinfo）
        internal bool user_command_enabled_sinfo = true;
        [ConfigOption] //コマンドの個別有効化（.079nuke）
        internal bool user_command_enabled_079nuke = true;
        [ConfigOption] //コマンドの個別有効化（.939sp）
        internal bool user_command_enabled_939sp = true;
        [ConfigOption] //コマンドの個別有効化（.079sp）
        internal bool user_command_enabled_079sp = true;
        [ConfigOption] //コマンドの個別有効化（.radio）
        internal bool user_command_enabled_radio = true;
        [ConfigOption] //コマンドの個別有効化（.radioのintercom機能）
        internal bool user_command_enabled_radio_intercom = true;
        [ConfigOption] //コマンドの個別有効化（.attack）
        internal bool user_command_enabled_attack = true;
        [ConfigOption] //コマンドの個別有効化（.boost）
        internal bool user_command_enabled_boost = true;

        //SCP系
        [ConfigOption] //発電機が起動完了した場合開かないように
        internal bool generator_engaged_cantopen = false;
        [ConfigOption] //SCP-049の周辺でSCP-049-2が回復する範囲
        internal float scp049_healing_to_049_2_range = -1f;
        [ConfigOption] //SCP-049の周辺でSCP-049-2が回復する量（毎秒）
        internal int scp049_healing_to_049_2_amount = -1;
        [ConfigOption] //SCP-049が治療成功時に周りのSCPが回復する範囲
        internal float scp049_healing_to_other_scp_range = -1f;
        [ConfigOption] //SCP-049が治療成功時に周りのSCPが回復する量
        internal int scp049_healing_to_other_scp_amount = -1;
        [ConfigOption] //SCP-079だけになった際発電機ロックがフリー&Tier5に
        internal bool scp079_lone_boost = false;
        [ConfigOption] //SCP-079だけになった際死ぬように
        internal bool scp079_lone_death = false;
        [ConfigOption] //SCP-079がロックダウン時全館停電を起こせるTier
        internal int scp079_all_flick_light_tier = -1;
        [ConfigOption] //SCP-079がスピーカー使用時に電力を使わなくなる
        internal bool scp079_speaker_no_ap_use = false;
        [ConfigOption] //SCP-096の発狂中に見られている場合の怒り加算への乗算値
        internal float scp096_enraged_increase_rage = -1f;
        [ConfigOption] //SCP-096にダメージを与えた際に発狂開始する
        internal bool scp096_damage_trigger = false;
        [ConfigOption] //SCP-106のポータルを踏むと人間がポケディメ行きに
        internal bool scp106_portal_trap_human = false;
        [ConfigOption] //SCP-106のポータルを踏むとSCPが106の元へワープできるように
        internal bool scp106_portal_warp_scp = false;
        [ConfigOption] //ポータル機能の最初のクールタイム
        internal int scp106_portal_to_human_wait = 180;
        [ConfigOption] //SCP-106のポケディメで人が死んだ際にマーク表示
        internal bool scp106_hitmark_pocket_death = false;
        [ConfigOption] //SCP-106のポケディメ内での人間がメディキット使用時回復量
        internal int scp106_pocket_medkit_recovery_amount = -1;
        [ConfigOption] //SCP-173が被弾時にまばたきを起こす確率
        internal int scp173_hurt_blink_percent = -1;
        [ConfigOption] //SCP-939の死体捕食を発生させるか
        internal bool scp939_killed_ragdoll_clean = false;
        [ConfigOption] //SCP-939のキル時加速
        internal float scp939_killed_speedup_multiplier = -1f;
        [ConfigOption] //SCP-939に出血ダメージを付与
        internal int scp939_dot_damage = -1;
        [ConfigOption] //SCP-939の出血ダメージの総量
        internal int scp939_dot_damage_total = 80;
        [ConfigOption] //SCP-939の出血間隔
        internal int scp939_dot_damage_interval = 1;
        [ConfigOption] //SCP-914にプレイヤーが入った際の挙動を少し変更
        internal bool scp914_changing = false;
        [ConfigOption] //SCP-049-2がキルした際もSCP-049が治療可能に
        internal bool infect_by_scp049_2 = false;
        [ConfigOption] //SCP-049が治療できなくなるまでの時間
        internal int infect_limit_time = 4;

        //人間系
        [ConfigOption] //被拘束時にドア/エレベーターを操作不能に
        internal bool handcuffed_cantopen = false;
        [ConfigOption] //医療キットでdotダメージを停止
        internal bool medkit_stop_dot_damage = false;
        [ConfigOption] //グレネードでのヒットマーク
        internal bool grenade_hitmark = false;
        [ConfigOption] //D脱出時の追加アイテム
        internal int classd_escaped_additemid = -1;

        //独自要素
        [ConfigOption] //地上エリアの緊急終了シーケンスを実施する時間
        internal int outsidezone_termination_time = -1;
        [ConfigOption] //地上エリアの緊急終了シーケンスの対SCP倍率
        internal float outsidezone_termination_multiplier_scp = 3.0f;
        [ConfigOption] //切断したSCPが再接続で戻るように
        internal bool scp_disconnect_at_resetrole = false;
        [ConfigOption] //自殺時に武器を持つのが必要に
        internal bool suicide_need_weapon = false;
        [ConfigOption] //核起動ボタンの蓋を自動で閉まるように & 核起動室の扉をEXIT_ACC持ち（中尉以上）で開けられるように (-1で無効)
        internal float nuke_button_auto_close = -1f;
        [ConfigOption] //核起爆後は増援が出ないように
        internal bool stop_mtf_after_nuke = false;
        [ConfigOption] //増援の場所を変更する確率
        internal int mtf_and_ci_change_spawnpoint = -1;
        [ConfigOption] //カードを持たなくても使用可能に
        internal bool inventory_card_act = false;
        [ConfigOption] //NTFになる際の脱出地点を変更
        internal bool escape_spawn = false;
        [ConfigOption] //放送室のモニターに情報表示
        internal bool intercom_information = false;
        [ConfigOption] //NTF/CIが被拘束で脱出地点に行くと敵対陣営になれるための残存味方勢力リミット
        internal int traitor_limitter = -1;
        [ConfigOption] //敵対陣営になるチャレンジの成功率
        internal int traitor_chance_percent = 50;

        //ダメージ系
        [ConfigOption] //指定以下の落下ダメージを無効にする
        internal float fallen_limit = 10.0f;
        [ConfigOption] //USPの対人間ダメージ乗算値
        internal float usp_damage_multiplier_human = 2.5f;
        [ConfigOption] //USPの対SCPダメージ乗算値
        internal float usp_damage_multiplier_scp = 5.0f;
        [ConfigOption] //被拘束者が受けるダメージの減算値
        internal float damage_divisor_cuffed = 1.0f;
        [ConfigOption] //SCP-173が受けるダメージの減算値
        internal float damage_divisor_scp173 = 1.0f;
        [ConfigOption] //SCP-106が受けるダメージの減算値
        internal float damage_divisor_scp106 = 1.0f;
        [ConfigOption] //SCP-106が受けるFRAGダメージ減算値
        internal float damage_divisor_scp106_grenade = 1.0f;
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


        //--------------------------LangOption--------------------------
        [LangOption] //SteamがLimitedの場合のメッセージ
        public readonly string steam_limited_message = "Your Steam account is Limited User Account.\\nThis server is not allowed Limited User.";
        [LangOption] //Levelが-1の場合
        public readonly string level_redacted_message = "???";
        [LangOption] //MOTDメッセージ（通常時）
        public readonly string motd_message = "[name], Welcome to our server!";
        [LangOption] //MOTDメッセージ（指定role時）
        public readonly string motd_role_message = "[name], Welcome to our server!\\nYou are VIP.";
        [LangOption] //SCPで切断時の復帰メッセージ
        public readonly string scp_rejoin_message = "Returns to the SCP from which it was disconnected.";
        [LangOption] //発電機起動開始
        public readonly string generator_starting_message = "Generator[[genname]] has starting.";
        [LangOption] //発電機起動完了
        public readonly string generator_complete_message = "[cur] out of [max] generators activated. [[genname]]";
        [LangOption] //発電機全起動完了
        public readonly string generator_readyforall_message = "[cur] out of [max] generators activated. [[genname]]\\nAll generators has been sucessfully engaged.\\nFinalizing recontainment sequence.\\nHeavy containment zone will overcharge in t-minus 1 minutes.";
        [LangOption] //LCZ閉鎖時のメッセージ
        public readonly string decontaminated_message = "Light Containment Zone is locked down and ready for decontamination. The removal of organic substances has now begun.";
        [LangOption] //核カウントダウン開始初回時のメッセージ
        public readonly string alphawarhead_countdown_start_message = "Alpha Warhead emergency detonation sequence engaged by [[name]/[role]].\\nThe underground section of the facility will be detonated in t-minus [second] seconds.";
        [LangOption] //核カウントダウン開始初回時のメッセージ（サーバー/コマンド/自動）
        public readonly string alphawarhead_countdown_start_server_message = "Alpha Warhead emergency detonation sequence engaged by Facility-Systems.\\nThe underground section of the facility will be detonated in t-minus [second] seconds.";
        [LangOption] //核カウントダウン再開時のメッセージ
        public readonly string alphawarhead_countdown_resume_message = "Detonation sequence resumed by [[name]/[role]]. t-minus [second] seconds.";
        [LangOption] //核カウントダウン再開時のメッセージ（サーバー/コマンド/自動）
        public readonly string alphawarhead_countdown_resume_server_message = "Detonation sequence resumed by Facility-Systems. t-minus [second] seconds.";
        [LangOption] //核カウントダウン中断時のメッセージ
        public readonly string alphawarhead_countdown_stop_message = "Detonation cancelled by [[name]/[role]]. Restarting systems.";
        [LangOption] //核カウントダウン中断時のメッセージ（サーバー/コマンド/自動）
        public readonly string alphawarhead_countdown_stop_server_message = "Detonation cancelled by Facility-Systems. Restarting systems.";
        [LangOption] //MTFスポーン時のメッセージ
        public readonly string mtfspawn_message = "Mobile Task Force Unit, Epsilon-11, designated, '[unit]', has entered the facility.\\nAll remaining personnel are advised to proceed with standard evacuation protocols until an MTF squad reaches your destination.\\nAwaiting recontainment of: [amount] SCP subject.";
        [LangOption] //MTFスポーン時のメッセージ（残SCPなし）
        public readonly string mtfspawn_noscp_message = "Mobile Task Force Unit, Epsilon-11, designated, '[unit]', has entered the facility.\\nAll remaining personnel are advised to proceed with standard evacuation protocols, until MTF squad has reached your destination.\\nSubstantial threat to safety is within the facility -- Exercise caution.";
        [LangOption] //FF時のメッセージ
        public readonly string friendlyfire_message = "Check your fire! Damage to ally forces not be tolerated.(Damaged to [[name]])";
        [LangOption] //囮放送時のメッセージ
        public readonly string lure_intercom_message = "You will be used for recontainment SCP-106. You can broadcast for [second] seconds.";
        [LangOption] //SCP収容成功時のメッセージ（標準）
        public readonly string scp_containment_message = "[role] contained successfully. Containment unit:[[unit]/[name]]";
        [LangOption] //SCP収容成功時のメッセージ（テスラ）
        public readonly string scp_containment_tesla_message = "[role] successfully terminated by automatic security system.";
        [LangOption] //SCP収容成功時のメッセージ（核）
        public readonly string scp_containment_alphawarhead_message = "[role] terminated by alpha warhead.";
        [LangOption] //SCP収容成功時のメッセージ（下層）
        public readonly string scp_containment_decont_message = "[role] lost in decontamination sequence.";
        [LangOption] //SCP収容成功時のメッセージ（不明）
        public readonly string scp_containment_unknown_message = "[role] contained successfully. Containment unit:[Unknown/[name]]";
        [LangOption] //SCP-049治療失敗時のメッセージ（既リスポーン）
        public readonly string scp049_recall_failed = "Recall failed. This player has already respawned.";
        [LangOption] //地上エリアの緊急終了シーケンス開始時
        public readonly string outsidezone_termination_message = "Danger, Outside Zone emergency termination sequence activated.";


        [LangOption] //発電機の名前（EZチェックポイント）
        public readonly string generator_ez_checkpoint_name = "Entrance Checkpoint";
        [LangOption] //発電機の名前（HCZ弾薬庫）
        public readonly string generator_hcz_armory_name = "HCZ Armory";
        [LangOption] //発電機の名前（サーバールーム）
        public readonly string generator_server_room_name = "Server Room";
        [LangOption] //発電機の名前（MicroHIDルーム）
        public readonly string generator_microhid_room_name = "MicroHID Room";
        [LangOption] //発電機の名前（SCP-049エレベーター前）
        public readonly string generator_scp049_name = "SCP-049 Elevator";
        [LangOption] //発電機の名前（SCP-079収容室）
        public readonly string generator_scp079_name = "SCP-079 Chamber";
        [LangOption] //発電機の名前（SCP-096収容室）
        public readonly string generator_scp096_name = "SCP-096 Chamber";
        [LangOption] //発電機の名前（SCP-106収容室）
        public readonly string generator_scp106_name = "SCP-106 Chamber";
        [LangOption] //発電機の名前（SCP-939収容室）
        public readonly string generator_scp939_name = "SCP-939 Chamber";
        [LangOption] //発電機の名前（核格納庫）
        public readonly string generator_nuke_name = "Nuke Chamber";


        [LangOption] //コマンドリジェクト時（Toofast)
        public readonly string user_command_rejected_toofast = "Command Rejected.(Too fast)";
        [LangOption] //コマンドリジェクト時（ラウンド中ではない）
        public readonly string user_command_rejected_not_round = "Command Rejected.(Can only used while round in progress)";
        [LangOption] //コマンドリジェクト時（条件不足）
        public readonly string user_command_rejected_miss_condition = "Command Rejected.(Condition is not met)";
        [LangOption] //コマンド成功時（.kill)
        public readonly string user_command_kill_success = "You suicided.";
        [LangOption] //コマンド（放送の終了)
        public readonly string user_command_boost_success = "Used [role] boost. ([hp]HP)";
        [LangOption] //コマンド（ラジオのオン)
        public readonly string user_command_radio_on = "Enabled Radio.";
        [LangOption] //コマンド（ラジオのオフ)
        public readonly string user_command_radio_off = "Disabled Radio.";
        [LangOption] //コマンド（放送の開始)
        public readonly string user_command_broadcast_start = "Starting broadcast.";
        [LangOption] //コマンド（放送の終了)
        public readonly string user_command_broadcast_stop = "Stopped broadcast.";

        public override void OnDisable()
        {
            Info("さにゃぷらぐいん Disabled");
        }

        public override void OnEnable()
        {
            SanyaPlugin.plugin = this;
            Info("さにゃぷらぐいん Loaded [Ver" + this.Details.version + "]");
            Info("ずり");

            Assembly aslib = Assembly.GetAssembly(typeof(ServerConsole));
            var Smodinternal = aslib.GetType("ServerMod");
            string smodver = Smodinternal.GetField("SM_VERSION", BindingFlags.Public | BindingFlags.Static).GetValue(null) as string;
            List<int> smodvers = new List<int>();
            foreach(var i in smodver.Split('.'))
            {
                smodvers.Add(int.Parse(i));
            }

            Info($"CurrentSmod:{smodvers[0]}.{smodvers[1]}.{smodvers[2]} / SanyaPluginTarget:{this.Details.SmodMajor}.{this.Details.SmodMinor}.{this.Details.SmodRevision}");
            if(smodvers[0] != this.Details.SmodMajor 
                || smodvers[1] != this.Details.SmodMinor 
                || smodvers[2] != this.Details.SmodRevision
                )
            {
                Error("SModとSanyaPluginのターゲットVerが相違しています。正しく動作しない場合があります。");
                Error("En:[SMod and SanyaPlugin target versions are different. may not work correctly.]");
            }
        }

        public override void Register()
        {
            AddCommand("sanya", new CommandHandler(this));
            AddEventHandlers(new EventHandler(this), Smod2.Events.Priority.Highest);
        }

        public void LoadPlayersData()
        {
            try
            {
                if(!Directory.Exists(FileManager.GetAppFolder(false) + "SanyaPlugin"))
                {
                    Directory.CreateDirectory(FileManager.GetAppFolder(false) + "SanyaPlugin");
                }
                if(!File.Exists(FileManager.GetAppFolder(false) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json"))
                {
                    File.WriteAllText(FileManager.GetAppFolder(false) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json", "[\n]");
                }
                playersData = JsonConvert.DeserializeObject<List<PlayerData>>(File.ReadAllText(FileManager.GetAppFolder(false) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json"));
                Info($"playersData Loaded.[Count:{playersData.Count}]");
            }
            catch(System.Exception e)
            {
                this.Error($"[DataLoader] {e.Message}");
            }
        }

        public void SavePlayersData()
        {
            try
            {
                if(!Directory.Exists(FileManager.GetAppFolder(false) + "SanyaPlugin"))
                {
                    Directory.CreateDirectory(FileManager.GetAppFolder(false) + "SanyaPlugin");
                }
                if(!File.Exists(FileManager.GetAppFolder(false) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json"))
                {
                    File.WriteAllText(FileManager.GetAppFolder(false) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json", "[\n]");
                }
                File.WriteAllText(FileManager.GetAppFolder(false) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json", JsonConvert.SerializeObject(playersData, Newtonsoft.Json.Formatting.Indented));
                Info($"playersData Saved.[Count:{playersData.Count}]");
            }
            catch(System.Exception e)
            {
                this.Error($"[DataSaver] {e.Message}");
            }
        }

        public string[] TranslateGeneratorName(RoomType type)
        {
            switch(type)
            {
                case RoomType.ENTRANCE_CHECKPOINT:
                    return new string[] { this.generator_ez_checkpoint_name, "Entrance Checkpoint" };
                case RoomType.HCZ_ARMORY:
                    return new string[] { this.generator_hcz_armory_name, "HCZ Armory" };
                case RoomType.SERVER_ROOM:
                    return new string[] { this.generator_server_room_name, "Server Room" };
                case RoomType.MICROHID:
                    return new string[] { this.generator_microhid_room_name, "MicroHID Room" };
                case RoomType.SCP_049:
                    return new string[] { this.generator_scp049_name, "SCP-049 Elevator" };
                case RoomType.SCP_079:
                    return new string[] { this.generator_scp079_name, "SCP-079 Chamber" };
                case RoomType.SCP_096:
                    return new string[] { this.generator_scp096_name, "SCP-096 Chamber" };
                case RoomType.SCP_106:
                    return new string[] { this.generator_scp106_name, "SCP-106 Chamber" };
                case RoomType.SCP_939:
                    return new string[] { this.generator_scp939_name, "SCP-939 Chamber" };
                case RoomType.NUKE:
                    return new string[] { this.generator_nuke_name, "Nuke Chamber" };
                default:
                    return new string[] { "???", "Unknown" };
            }
        }

        static public int GetRandomIndexFromWeight(int[] list)
        {
            System.Random rnd = new System.Random();
            int sum = 0;

            foreach(int i in list)
            {
                if(i <= 0) continue;
                sum += i;
            }

            int random = rnd.Next(0, sum);
            for(int i = 0; i < list.Length; i++)
            {
                if(list[i] <= 0) continue;

                if(random < list[i])
                {
                    return i;
                }
                random -= list[i];
            }
            return -1;
        }

        static public float GetDamageFromHitbox(HitboxIdentity hitbox, float damage)
        {
            switch(hitbox.id.ToUpper())
            {
                case "HEAD":
                    return damage *= 4f;
                case "LEG":
                    return damage /= 2f;
                case "SCP106":
                    return damage /= 10f;
                default:
                    return damage;
            }
        }

        static public Vector GetCameraPosByName(string name)
        {
            foreach(Camera079 camera in Scp079PlayerScript.allCameras)
            {
                if(camera.cameraName == name)
                {
                    return new Vector(camera.transform.position.x, camera.transform.position.y, camera.transform.position.z);
                }
            }
            return null;
        }

        static public Vector GetRoomPosByRoomid(string roomid)
        {
            foreach(GameObject item in GameObject.FindGameObjectsWithTag("RoomID"))
            {
                Rid rid = item.GetComponent<Rid>();
                if(rid != null && (rid.id == roomid))
                {
                    return new Vector(item.transform.position.x, item.transform.position.y, item.transform.position.z);
                }
            }
            return null;
        }

        static public bool CanOpenDoor(string[] permission, Smod2.API.Door door)
        {
            Door targetDoor = door.GetComponent() as Door;

            if(targetDoor != null)
            {
                if(targetDoor.permissionLevel.Length == 0)
                {
                    return true;
                }

                foreach(string item in permission)
                {
                    if(item.Contains(targetDoor.permissionLevel))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static public bool CanOpenDoor(string[] permission, string[] target)
        {
            if(target.Length == 0)
            {
                return true;
            }

            foreach(string t in target)
            {
                foreach(string p in permission)
                {
                    if(t.Contains(p))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static public void SetMute(Player player, bool b)
        {
            (player.GetGameObject() as UnityEngine.GameObject).GetComponent<CharacterClassManager>().SetMuted(b);
        }

        static public void CloseBlastDoor()
        {
            foreach(BlastDoor item in UnityEngine.Object.FindObjectsOfType<BlastDoor>())
            {
                item.SetClosed(true);
            }
        }

        static public void Call914Use(Player player = null)
        {
            if(player != null)
            {
                GameObject gameObject = player.GetGameObject() as GameObject;
                Scp914.singleton.Set914User(gameObject);
                gameObject.GetComponent<PlayerInteract>().CallRpcUse914();
            }
            else
            {
                GameObject host = GameObject.Find("Host");
                Scp914.singleton.Set914User(host);
                host.GetComponent<PlayerInteract>().CallRpcUse914();
            }
        }

        static public void Call914Change(Player player = null)
        {
            if(player != null)
            {
                GameObject gameObject = player.GetGameObject() as GameObject;
                Scp914.singleton.Set914User(gameObject);
                Scp914.singleton.ChangeKnobStatus(gameObject);
            }
            else
            {
                GameObject host = GameObject.Find("Host");
                Scp914.singleton.Set914User(host);
                Scp914.singleton.ChangeKnobStatus(host);
            }
        }

        static public void Call106Scream(Player player = null)
        {
            if(player != null)
            {
                GameObject gameObject = player.GetGameObject() as GameObject;
                gameObject.GetComponent<PlayerInteract>().CallRpcContain106(gameObject);
            }
            else
            {
                GameObject host = GameObject.Find("Host");
                host.GetComponent<PlayerInteract>().CallRpcContain106(host);
            }
        }

        static public Vector Call106PDExit()
        {
            List<Vector3> list = new List<Vector3>();
            GameObject[] pdexits = GameObject.FindGameObjectsWithTag("PD_EXIT");
            foreach(GameObject exits in pdexits)
            {
                list.Add(exits.transform.position);
            }
            int rand = UnityEngine.Random.Range(0, list.Count);
            Vector pos = new Vector(list[rand].x, list[rand].y + 2f, list[rand].z);
            return pos;
        }

        static public List<Vector> Call106PDRandomExit(bool isAlreadyDecont = false)
        {
            List<Vector> list = new List<Vector>();
            GameObject[] roomids = GameObject.FindGameObjectsWithTag("RoomID");
            foreach(GameObject room in roomids)
            {
                if(room.GetComponent<Rid>() != null
                    && (!isAlreadyDecont || !(room.transform.position.y < 100 && room.transform.position.y > -100)))
                {
                    list.Add(new Vector(room.transform.position.x, room.transform.position.y + 2, room.transform.position.z));
                }
            }

            return list;
        }

        static public void Call173SnapSound(Player player)
        {
            (player.GetGameObject() as UnityEngine.GameObject).GetComponent<Scp173PlayerScript>().CallRpcSyncAudio();
        }

        static public void Call173Blink()
        {
            FlashEffect[] array = UnityEngine.Object.FindObjectsOfType<FlashEffect>();
            foreach(FlashEffect flasheffect in array)
            {
                flasheffect.Networksync_blind = true;
            }

            Scp173PlayerScript[] array2 = UnityEngine.Object.FindObjectsOfType<Scp173PlayerScript>();
            foreach(Scp173PlayerScript scp173PlayerScript in array2)
            {
                scp173PlayerScript.CallRpcBlinkTime();
            }
        }

        static public void Call939SetSpeedMultiplier(Player player, float multiplier)
        {
            (player.GetGameObject() as UnityEngine.GameObject).GetComponent<Scp939PlayerScript>().NetworkspeedMultiplier = multiplier;
        }

        static public void Call939CanSee()
        {
            List<Scp939_VisionController> list939 = new List<Scp939_VisionController>(UnityEngine.GameObject.FindObjectsOfType<Scp939_VisionController>()).FindAll(x => x.name.Contains("Player"));

            foreach(Scp939_VisionController i in list939)
            {
                i.MakeNoise(100f);
            }
        }

        static public void CallVehicle(bool isCi)
        {
            UnityEngine.GameObject gameObject = UnityEngine.GameObject.Find("Host");

            if(gameObject != null)
            {
                if(isCi)
                {
                    gameObject.GetComponent<MTFRespawn>().CallRpcVan();
                }
                else
                {
                    ChopperAutostart chop = UnityEngine.Object.FindObjectOfType<ChopperAutostart>();

                    if(!chop.NetworkisLanded)
                    {
                        chop.SetState(true);
                    }
                    else
                    {
                        chop.SetState(false);
                    }
                }
            }
        }

        static public void CallMTFSpawn(bool isCi)
        {
            UnityEngine.GameObject gameObject = UnityEngine.GameObject.Find("Host");

            if(gameObject != null)
            {
                gameObject.GetComponent<MTFRespawn>().nextWaveIsCI = isCi;
                gameObject.GetComponent<MTFRespawn>().timeToNextRespawn = 0.1f;
            }
        }

        static public void CallAmbientSound(int id)
        {
            UnityEngine.GameObject.Find("Host").GetComponent<AmbientSoundPlayer>().CallRpcPlaySound(UnityEngine.Mathf.Clamp(id, 0, 31));
        }

        static public void CallAlphaWarhead(Player player = null)
        {
            if(!AlphaWarheadController.host.inProgress)
            {
                AlphaWarheadController.host.InstantPrepare();
                AlphaWarheadController.host.StartDetonation(player?.GetGameObject() as GameObject);
            }
            else
            {
                AlphaWarheadController.host.CancelDetonation(player?.GetGameObject() as GameObject);
            }
        }

        static public void CallRpcTargetShake(Player target)
        {
            GameObject gameObject = target.GetGameObject() as GameObject;
            if(gameObject != null)
            {
                int rpcid = -737840022;

                UnityEngine.Networking.NetworkWriter writer = new UnityEngine.Networking.NetworkWriter();
                writer.Write((short)0);
                writer.Write((short)2);
                writer.WritePackedUInt32((uint)rpcid);
                writer.Write(gameObject.GetComponent<UnityEngine.Networking.NetworkIdentity>().netId);
                writer.FinishMessage();
                gameObject.GetComponent<CharacterClassManager>().connectionToClient.SendWriter(writer, 0);
            }
        }

        static public void CallRpcTargetDecontaminationLCZ(Player target)
        {
            GameObject gameObject = target.GetGameObject() as GameObject;
            if(gameObject != null)
            {
                int rpcid = -1569315677;

                UnityEngine.Networking.NetworkWriter writer = new UnityEngine.Networking.NetworkWriter();
                writer.Write((short)0);
                writer.Write((short)2);
                writer.WritePackedUInt32((uint)rpcid);
                writer.Write(gameObject.GetComponent<UnityEngine.Networking.NetworkIdentity>().netId);
                writer.WritePackedUInt32((uint)4);
                writer.Write(true);
                writer.FinishMessage();
                gameObject.GetComponent<CharacterClassManager>().connectionToClient.SendWriter(writer, 0);
            }
        }

        static public void Explode(Player attacker, Vector explode_posion, bool flashbang = false, bool effectonly = false)
        {
            GrenadeManager gm = PlayerManager.localPlayer.GetComponent<GrenadeManager>();
            int pid = attacker.PlayerId;
            string gid = "SERVER_" + pid + ":" + (gm.smThrowInteger + 4096);
            gm.CallRpcThrowGrenade(flashbang ? 1 : 0, pid, gm.smThrowInteger++ + 4096, new Vector3(0f, 0f, 0f), true, effectonly ? new Vector3(0f, 0f, 0f) : new Vector3(explode_posion.x, explode_posion.y, explode_posion.z), false, 0);
            gm.CallRpcUpdate(gid, new Vector3(explode_posion.x, explode_posion.y, explode_posion.z), Quaternion.Euler(Vector3.zero), Vector3.zero, Vector3.zero);
            gm.CallRpcExplode(gid, pid);
        }

        static public void ShowHitmarker(Player target)
        {
            GameObject gameObject = target.GetGameObject() as GameObject;
            if(gameObject != null)
            {
                int rpcid = -986408589;

                UnityEngine.Networking.NetworkWriter writer = new UnityEngine.Networking.NetworkWriter();
                writer.Write((short)0);
                writer.Write((short)2);
                writer.WritePackedUInt32((uint)rpcid);
                writer.Write(gameObject.GetComponent<UnityEngine.Networking.NetworkIdentity>().netId);
                writer.Write(true);
                writer.WritePackedUInt32((uint)0);
                writer.FinishMessage();
                gameObject.GetComponent<CharacterClassManager>().connectionToClient.SendWriter(writer, 0);
            }
        }

        static public void SetExtraDoorNames()
        {
            foreach(Door item in UnityEngine.Object.FindObjectsOfType<Door>())
            {
                if(item.name.Contains("ContDoor"))
                {
                    if(item.DoorName.Length == 0)
                    {
                        if(item.transform.parent.name.Contains("MeshDoor173"))
                        {
                            item.DoorName = "173_CONTAIN";
                        }
                        else if(item.transform.parent.name.Contains("Entrance Door"))
                        {
                            item.DoorName = "049_CONTAIN";
                        }
                        else if(item.transform.parent.name.Contains("Shelter"))
                        {
                            item.DoorName = "SHELTER";
                        }
                    }
                }
                else if(item.name.Contains("PrisonDoor"))
                {
                    if(!item.name.Contains("("))
                    {
                        item.DoorName = item.name.Replace("Door", "_0").ToUpper();
                    }
                    else
                    {
                        item.DoorName = item.name.Replace("Door", "").Replace(" (", "_").Replace(")", "").ToUpper();
                    }
                }
                else if(item.name.Contains("LightContainmentDoor"))
                {
                    if(item.transform.parent.name.Contains("372"))
                    {
                        item.DoorName = "372_CONTAIN";
                    }
                    else if(item.transform.parent.name.Contains("Map_LC_Toilets"))
                    {
                        item.DoorName = "WC";
                    }
                    else if(item.transform.parent.name.Contains("Map_HC_079CR"))
                    {
                        item.DoorName = "079_INNER";
                    }
                    else if(item.transform.parent.name.Contains("Servers"))
                    {
                        item.DoorName = "SERVER";
                    }
                    else if(item.transform.parent.name.Contains("Root_Airlock"))
                    {
                        if(!item.transform.parent.name.Contains("(1)"))
                        {
                            item.DoorName = "AIRLOCK_0";
                        }
                        else
                        {
                            item.DoorName = "AIRLOCK_1";
                        }
                    }
                    else if(item.transform.parent.name.Contains("All"))
                    {
                        if(item.name.Contains("(33)"))
                        {
                            item.DoorName = "939_BOTTOM_LEFT";
                        }
                        else if(item.name.Contains("(31)"))
                        {
                            item.DoorName = "939_BOTTOM_RIGHT";
                        }
                        else if(item.name.Contains("(30)"))
                        {
                            item.DoorName = "939_UP_LEFT";
                        }
                        else if(item.name.Contains("(29)"))
                        {
                            item.DoorName = "939_UP_RIGHT";
                        }
                    }
                }
            }
        }

        static public void SetExtraPermission(string name, string permission)
        {
            foreach(Door item in UnityEngine.Object.FindObjectsOfType<Door>())
            {
                if(item.DoorName.Contains(name))
                {
                    item.permissionLevel = permission;
                }
            }
        }

        static public Vector GetSpawnElevatorPos(Elevator[] elevators, bool isCi, bool isReSpawn)
        {
            foreach(Elevator i in elevators)
            {
                if(i.ElevatorType == ElevatorType.GateA && isCi)
                {
                    foreach(Vector x in i.GetPositions())
                    {
                        if((int)System.Math.Truncate(x.y) == 1001 && isReSpawn)
                        {
                            return x;
                        }
                        else if((int)System.Math.Truncate(x.y) == -998 && !isReSpawn)
                        {
                            return x;
                        }
                    }
                }
                else if(i.ElevatorType == ElevatorType.GateB && !isCi)
                {
                    foreach(Vector x in i.GetPositions())
                    {
                        if((int)System.Math.Truncate(x.y) == 994)
                        {
                            return x;
                        }
                        else if((int)System.Math.Truncate(x.y) == -998 && !isReSpawn)
                        {
                            return x;
                        }
                    }
                }
            }
            return null;
        }

        public IEnumerator<float> _CheckIsLimitedSteam(Player player, bool announcement = true)
        {
            var targetdata = this.playersData.Find(x => x.steamid == player.SteamId);

            if(this.data_enabled)
            {
                if(targetdata != null && !targetdata.limited)
                {
                    Info($"[SteamCheck] Already Checked:{player.SteamId}");
                    yield break;
                }
            }

            using(WWW www = new WWW("https://steamcommunity.com/profiles/" + player.SteamId + "?xml=1"))
            {
                yield return Timing.WaitUntilDone(www);
                if(string.IsNullOrEmpty(www.error))
                {
                    XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                    xmlReaderSettings.IgnoreComments = true;
                    xmlReaderSettings.IgnoreWhitespace = true;
                    XmlReader xmlReader = XmlReader.Create(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(www.text)), xmlReaderSettings);

                    while(xmlReader.Read())
                    {
                        if(xmlReader.ReadToFollowing("isLimitedAccount"))
                        {
                            string isLimited = xmlReader.ReadElementContentAsString();
                            if(isLimited == "0")
                            {
                                Info($"[SteamCheck] OK.[NotLimited]:{player.SteamId}");
                                if(this.data_enabled && targetdata != null)
                                {
                                    targetdata.limited = false;
                                }
                                yield break;
                            }
                            else
                            {
                                Warn($"[SteamCheck] NG.[Limited]:{player.SteamId}");
                                ServerConsole.Disconnect(player.GetGameObject() as GameObject, this.steam_limited_message);
                                yield break;
                            }
                        }
                        else
                        {
                            Warn($"[SteamCheck] Falied.[NoProfile]:{player.SteamId}");
                            ServerConsole.Disconnect(player.GetGameObject() as GameObject, this.steam_limited_message);
                            yield break;
                        }
                    }
                }
                else
                {
                    Error($"[SteamCheck] Error.[{www.error}]");
                    yield break;
                }
            }
            yield break;
        }

        static public IEnumerator<float> _Emulate079Recontain(bool announcement = true)
        {
            yield return Timing.WaitForSeconds(1f);

            MTFRespawn mtf = PlayerManager.localPlayer.GetComponent<MTFRespawn>();
            PlayerStats ps = PlayerManager.localPlayer.GetComponent<PlayerStats>();
            NineTailedFoxAnnouncer annc = NineTailedFoxAnnouncer.singleton;
            while(annc.queue.Count > 0 || AlphaWarheadController.host.inProgress)
            {
                yield return 0f;
            }
            if(announcement)
            {
                mtf.CallRpcPlayCustomAnnouncement("SCP079RECON6", true);
                mtf.CallRpcPlayCustomAnnouncement((Scp079PlayerScript.instances.Count <= 0) ? "FACILITY IS BACK IN OPERATIONAL MODE" : "SCP 0 7 9 CONTAINEDSUCCESSFULLY", false);
            }
            for(int i = 0; i < 350; i++)
            {
                yield return 0f;
            }

            Generator079.generators[0].CallRpcOvercharge();
            Door[] array = UnityEngine.Object.FindObjectsOfType<Door>();
            foreach(Door door in array)
            {
                Scp079Interactable component = door.GetComponent<Scp079Interactable>();
                Scp079Interactable.ZoneAndRoom zoneAndRoom = component.currentZonesAndRooms[0];
                if(zoneAndRoom.currentZone == "HeavyRooms" && door.isOpen && !door.locked)
                {
                    door.ChangeState(true);
                }
            }

            Recontainer079.isLocked = true;
            foreach(Scp079PlayerScript scp079PlayerScript in Scp079PlayerScript.instances)
            {
                ps.HurtPlayer(new PlayerStats.HitInfo(1000001f, "WORLD", DamageTypes.Tesla, 0), scp079PlayerScript.gameObject);
            }
            for(int i = 0; i < 500; i++)
            {
                yield return 0f;
            }
            Recontainer079.isLocked = false;

            yield break;
        }

        static public IEnumerator<float> _SummaryLessRoundrestart(SanyaPlugin plugin, int restarttime)
        {
            yield return Timing.WaitForSeconds(restarttime);
            RoundSummary.singleton.CallRpcDimScreen();
            ServerConsole.AddLog("Round restarting");
            plugin.Round.RestartRound();
            yield break;
        }

        static public IEnumerator<float> _DelayedRecall(Player player, float delay = 10.0f)
        {
            CharacterClassManager ccm = (player.GetGameObject() as GameObject).GetComponent<CharacterClassManager>();
            yield return Timing.WaitForSeconds(delay);

            foreach(var ragdoll in GameObject.FindObjectsOfType<Ragdoll>())
            {
                if(ragdoll.owner.PlayerId == player.PlayerId && ragdoll.CompareTag("Ragdoll"))
                {
                    UnityEngine.Networking.NetworkServer.Destroy(ragdoll.gameObject);
                }
            }
            if(ccm.curClass == (int)Role.SPECTATOR)
            {
                player.ChangeRole(Role.SCP_049_2, true, false, false, false);
            }
            yield break;
        }

        static public IEnumerator<float> _DelayedAddItem(Player player, ItemType item)
        {
            yield return Timing.WaitForSeconds(0.1f);

            player.GiveItem(item);

            yield break;
        }

        static public IEnumerator<float> _DelayedGrantedLevel(Smod2.API.Player player, PlayerData data)
        {
            yield return Timing.WaitForSeconds(1f);

            ServerRoles serverRoles = (player.GetGameObject() as GameObject).GetComponent<ServerRoles>();
            string hasRank = serverRoles.GetUncoloredRoleString();
            string hasColor = serverRoles.MyColor;
            if(hasRank.Contains("Patreon"))
            {
                hasRank = "Patreon";
            }
            if(hasColor == "light_red")
            {
                hasColor = "pink";
            }

            string level = "";

            if(data.level == -1)
            {
                level = plugin.level_redacted_message;
            }
            else
            {
                level = data.level.ToString();
            }

            if(!string.IsNullOrEmpty(hasRank))
            {
                player.SetRank(hasColor, $"Level{level} : {hasRank}", null);
            }
            else
            {
                player.SetRank(null, $"Level{level}", null);
            }

            yield break;
        }

        static public IEnumerator<float> _DelayedTeleport(Player player, Vector pos, bool unstack)
        {
            yield return Timing.WaitForSeconds(0.1f);
            player.Teleport(pos, unstack);
            yield break;
        }

        static public IEnumerator<float> _DelayedSuicideWithWeaponSound(Player player, DamageType cause, float delay = 0.25f)
        {
            WeaponManager wpm = (player.GetGameObject() as GameObject).GetComponent<WeaponManager>();
            wpm.CallRpcConfirmShot(false, wpm.curWeapon);
            yield return Timing.WaitForSeconds(delay);
            player.Kill(cause);
            yield break;
        }

        static public IEnumerator<float> _DeleyadCloseOutsideWarHeadCap(float delay = 10.0f)
        {
            yield return Timing.WaitForSeconds(0.1f);
            if(!GameObject.FindObjectOfType<AlphaWarheadOutsitePanel>().keycardEntered)
            {
                yield break;
            }
            yield return Timing.WaitForSeconds(delay);
            GameObject.FindObjectOfType<AlphaWarheadOutsitePanel>().SetKeycardState(false);
            yield break;
        }

        static public IEnumerator<float> _DelayedSetReSetRole(SCPPlayerData data, Player player)
        {
            yield return Timing.WaitForSeconds(1f);
            player.ChangeRole(data.role, false, false, false, false);
            yield return Timing.WaitForSeconds(0.1f);
            if(data.role == Role.SCP_079)
            {
                player.Scp079Data.Level = data.level079;
                player.Scp079Data.AP = data.ap079;
                player.Scp079Data.SetCamera(data.camera079);
            }
            else
            {
                player.Teleport(data.pos);
                player.SetHealth(data.health);
            }
            yield break;
        }

        static public IEnumerator<float> _DelayedFastSpawn(float fast)
        {
            yield return Timing.WaitForSeconds(0.2f);
            GameObject.Find("Host").GetComponent<MTFRespawn>().timeToNextRespawn /= fast;
            yield break;
        }

        static public IEnumerator<float> _AirSupportBomb(int waitforready = 5, int bombcount = 10, bool isCassie = true, bool isSubtitle = false, bool isManualEndOnly = false)
        {
            plugin.Warn($"[Airbomb] booting...");
            if(isAirBombGoing)
            {
                plugin.Error($"[Airbomb] already booted, cancel.");
                yield break;
            }
            else
            {
                isAirBombGoing = true;
            }

            if(isSubtitle)
            {
                plugin.Server.Map.ClearBroadcasts();
                plugin.Server.Map.Broadcast(10, plugin.outsidezone_termination_message, false);
            }

            if(isCassie)
            {
                plugin.Server.Map.AnnounceCustomMessage("danger outside zone emergency termination sequence activated");
                yield return Timing.WaitForSeconds(5f);
            }

            plugin.Warn($"[Airbomb] charging...");
            while(waitforready > 0)
            {
                SanyaPlugin.CallAmbientSound(7);
                waitforready--;
                yield return Timing.WaitForSeconds(1f);
            }

            plugin.Warn($"[Airbomb] throwing...");
            ServerConsole.FriendlyFire = true;
            Player hostplayer = new ServerMod2.API.SmodPlayer(GameObject.Find("Host"));
            int throwcount = 0;

            while((bombcount > 0 || isManualEndOnly) && isAirBombGoing)
            {
                List<Vector> randompos = new List<Vector>();
                randompos.Add(new Vector(UnityEngine.Random.Range(175, 182), 984, UnityEngine.Random.Range(25, 29)));
                randompos.Add(new Vector(UnityEngine.Random.Range(174, 182), 984, UnityEngine.Random.Range(36, 39)));
                randompos.Add(new Vector(UnityEngine.Random.Range(166, 174), 984, UnityEngine.Random.Range(26, 39)));
                randompos.Add(new Vector(UnityEngine.Random.Range(169, 171), 987, UnityEngine.Random.Range(9, 24)));
                randompos.Add(new Vector(UnityEngine.Random.Range(174, 175), 988, UnityEngine.Random.Range(10, -2)));
                randompos.Add(new Vector(UnityEngine.Random.Range(186, 174), 990, UnityEngine.Random.Range(-1, -2)));
                randompos.Add(new Vector(UnityEngine.Random.Range(186, 189), 991, UnityEngine.Random.Range(-1, -24)));
                randompos.Add(new Vector(UnityEngine.Random.Range(185, 189), 993, UnityEngine.Random.Range(-26, -34)));

                randompos.Add(new Vector(UnityEngine.Random.Range(180, 195), 995, UnityEngine.Random.Range(-36, -91)));
                randompos.Add(new Vector(UnityEngine.Random.Range(148, 179), 995, UnityEngine.Random.Range(-45, -72)));
                randompos.Add(new Vector(UnityEngine.Random.Range(118, 148), 995, UnityEngine.Random.Range(-47, -65)));
                randompos.Add(new Vector(UnityEngine.Random.Range(83, 118), 995, UnityEngine.Random.Range(-47, -65)));
                randompos.Add(new Vector(UnityEngine.Random.Range(13, 15), 995, UnityEngine.Random.Range(-18, -48)));

                randompos.Add(new Vector(UnityEngine.Random.Range(68, 83), 988, UnityEngine.Random.Range(-52, -66)));
                randompos.Add(new Vector(UnityEngine.Random.Range(53, 68), 988, UnityEngine.Random.Range(-53, -63)));
                randompos.Add(new Vector(UnityEngine.Random.Range(12, 49), 988, UnityEngine.Random.Range(-47, -66)));
                randompos.Add(new Vector(UnityEngine.Random.Range(38, 42), 988, UnityEngine.Random.Range(-40, -47)));
                randompos.Add(new Vector(UnityEngine.Random.Range(38, 43), 988, UnityEngine.Random.Range(-32, -38)));
                randompos.Add(new Vector(UnityEngine.Random.Range(-25, 12), 988, UnityEngine.Random.Range(-50, -66)));
                randompos.Add(new Vector(UnityEngine.Random.Range(-26, -56), 988, UnityEngine.Random.Range(-50, -66)));

                randompos.Add(new Vector(UnityEngine.Random.Range(-3, -24), 1001, UnityEngine.Random.Range(-66, -73)));
                randompos.Add(new Vector(UnityEngine.Random.Range(5, 28), 1001, UnityEngine.Random.Range(-66, -73)));
                randompos.Add(new Vector(UnityEngine.Random.Range(29, 55), 1001, UnityEngine.Random.Range(-66, -73)));
                randompos.Add(new Vector(UnityEngine.Random.Range(50, 54), 1001, UnityEngine.Random.Range(-49, -66)));
                randompos.Add(new Vector(UnityEngine.Random.Range(24, 48), 1001, UnityEngine.Random.Range(-41, -46)));
                randompos.Add(new Vector(UnityEngine.Random.Range(5, 24), 1001, UnityEngine.Random.Range(-41, -46)));
                randompos.Add(new Vector(UnityEngine.Random.Range(-4, -17), 1001, UnityEngine.Random.Range(-41, -46)));
                randompos.Add(new Vector(UnityEngine.Random.Range(4, -3), 1001, UnityEngine.Random.Range(-47, -73)));

                randompos.Add(new Vector(UnityEngine.Random.Range(4, -4), 1001, UnityEngine.Random.Range(-25, -40)));
                randompos.Add(new Vector(UnityEngine.Random.Range(11, -11), 1001, UnityEngine.Random.Range(-18, -21)));
                randompos.Add(new Vector(UnityEngine.Random.Range(3, -3), 1001, UnityEngine.Random.Range(-4, -17)));
                randompos.Add(new Vector(UnityEngine.Random.Range(2, 14), 1001, UnityEngine.Random.Range(3, -3)));
                randompos.Add(new Vector(UnityEngine.Random.Range(-1, -13), 1001, UnityEngine.Random.Range(4, -3)));

                randompos = randompos.OrderBy(x => Guid.NewGuid()).ToList();
                foreach(var pos in randompos)
                {
                    SanyaPlugin.Explode(hostplayer, pos, false, false);
                    yield return Timing.WaitForSeconds(0.1f);
                }

                bombcount--;
                throwcount++;
                yield return Timing.WaitForSeconds(0.25f);
            }

            if(isCassie)
            {
                plugin.Server.Map.AnnounceCustomMessage("outside zone termination sequence complete");
            }

            plugin.Warn($"AirBomb Ended. throwcount:{throwcount} status:{isAirBombGoing}");
            isAirBombGoing = false;
            ServerConsole.FriendlyFire = plugin.ConfigManager.Config.GetBoolValue("friendly_fire", false);
            yield break;
        }

        static public IEnumerator<float> _SuicideWithFollowingGrenade(Player attacker)
        {
            GrenadeManager gm = (attacker.GetGameObject() as GameObject).GetComponent<GrenadeManager>();
            string gid = "SERVER_" + attacker.PlayerId + ":" + (gm.smThrowInteger + 4096);
            gm.CallRpcThrowGrenade(0, attacker.PlayerId, gm.smThrowInteger++ + 4096, new Vector3(0f, 0f, 0f), true, new Vector3(0f, 0f, 0f), false, 0);
            float delta = 0f;
            while(delta < 4.6f)
            {
                delta += Timing.DeltaTime;
                gm.CallRpcUpdate(gid, new Vector3(attacker.GetPosition().x, attacker.GetPosition().y, attacker.GetPosition().z), Quaternion.Euler(Vector3.zero), Vector3.zero, Vector3.zero);
                yield return 0f;
            }
            gm.CallRpcExplode(gid, attacker.PlayerId);
            SanyaPlugin.Explode(attacker, new Vector(attacker.GetPosition().x, attacker.GetPosition().y - 1f, attacker.GetPosition().z), false);
            attacker.Kill(DamageType.FRAG);
            yield break;
        }

        static public IEnumerator<float> _EscapeEmulateForElevator(Elevator elevator)
        {
            yield return Timing.WaitForSeconds(2f);

            Lift lift = elevator.GetComponent() as Lift;
            foreach(Lift.Elevator ele in lift.elevators)
            {
                Collider[] coliders = Physics.OverlapBox(ele.target.transform.position, Vector3.one * lift.maxDistance, new Quaternion(0f, 0f, 0f, 0f), teslamask);
                foreach(Collider colider in coliders)
                {
                    PlayerStats ps = colider.GetComponentInParent<PlayerStats>();
                    if(ps != null)
                    {
                        Player player = new ServerMod2.API.SmodPlayer(ps.gameObject);
                        if(player.TeamRole.Role == Role.CLASSD)
                        {
                            RoundSummary.escaped_ds++;
                            if(player.IsHandcuffed())
                            {
                                player.ChangeRole(Role.NTF_CADET, true, false, true, true);
                            }
                            else
                            {
                                player.ChangeRole(Role.CHAOS_INSURGENCY, true, false, true, true);
                            }
                        }
                        else if(player.TeamRole.Role == Role.SCIENTIST)
                        {
                            RoundSummary.escaped_scientists++;
                            if(player.IsHandcuffed())
                            {
                                player.ChangeRole(Role.CHAOS_INSURGENCY, true, false, true, true);
                            }
                            else
                            {
                                player.ChangeRole(Role.NTF_SCIENTIST, true, false, true, true);
                            }
                        }
                    }
                }
            }

            yield return Timing.WaitForSeconds(7.5f);
            lift.UseLift();
            yield break;
        }

        static public IEnumerator<float> _BeforeSpawnMoving(Elevator elevator)
        {
            Lift lift = elevator.GetComponent() as Lift;
            lift.NetworkstatusID = (int)Lift.Status.Down;
            yield return Timing.WaitForSeconds(0.1f);
            elevator.Use();
            yield break;
        }

        static public IEnumerator<float> _DeductBatteryHasTransmission(Player player)
        {
            yield return Timing.WaitForSeconds(2.25f);

            while(true)
            {
                if(Intercom.host.speaker != null)
                {
                    ServerMod2.API.SmodPlayer ply = new ServerMod2.API.SmodPlayer(Intercom.host.speaker);

                    if(ply.PlayerId == player.PlayerId)
                    {
                        bool hasradio = false;
                        Inventory inv = (player.GetGameObject() as GameObject).GetComponent<Inventory>();

                        foreach(var i in inv.items)
                        {
                            if(i.id == (int)ItemType.RADIO)
                            {
                                hasradio = true;
                                player.SetRadioBattery((int)(i.durability) - 10);
                                if(i.durability < 0)
                                {
                                    PluginManager.Manager.Server.Map.SetIntercomSpeaker(null);
                                    player.SendConsoleMessage("battery 0");
                                    yield break;
                                }
                                break;
                            }
                        }

                        if(!hasradio)
                        {
                            PluginManager.Manager.Server.Map.SetIntercomSpeaker(null);
                            player.SendConsoleMessage("no radio");
                            yield break;
                        }
                    }
                }
                else
                {
                    player.SendConsoleMessage("breaked");
                    yield break;
                }
                yield return Timing.WaitForSeconds(0.5f);
            }
        }

        static public IEnumerator<float> _106CreatePortalEX(Player scp, Player human)
        {
            GameObject gameObject_scp = scp.GetGameObject() as GameObject;
            GameObject gameObject_human = human.GetGameObject() as GameObject;
            Scp106PlayerScript p106_scp = gameObject_scp.GetComponent<Scp106PlayerScript>();
            Scp106PlayerScript p106_human = gameObject_human.GetComponent<Scp106PlayerScript>();
            PlyMovementSync pms_scp = gameObject_human.GetComponent<PlyMovementSync>();
            PlyMovementSync pms_human = gameObject_human.GetComponent<PlyMovementSync>();

            pms_human.SetAllowInput(false);
            pms_scp.SetAllowInput(false);
            RaycastHit raycastHit;
            if(Physics.Raycast(new Ray(gameObject_human.transform.position, -gameObject_human.transform.up), out raycastHit, 10f, p106_scp.teleportPlacementMask))
            {
                p106_human.NetworkportalPosition = raycastHit.point - Vector3.up;
                p106_scp.NetworkportalPosition = raycastHit.point - Vector3.up;
            }

            yield return Timing.WaitForSeconds(0.1f);
            p106_scp.CallCmdUsePortal();
            p106_human.CallRpcTeleportAnimation();
            for(float i = 0f; i < 200; i++)
            {
                var pos = gameObject_human.transform.position;
                if(i > 180)
                {
                    pos.y -= i * 0.001f;
                }
                else
                {
                    pos.y -= i * 0.0001f;
                }
                pms_human.SetPosition(pos);
                yield return 0f;
            }
            if(AlphaWarheadController.host.doorsClosed)
            {
                human.Damage(500, DamageType.SCP_106);
            }
            else
            {
                human.Damage(40, DamageType.SCP_106);
            }
            pms_human.SetPosition(Vector3.down * 1997f);
            pms_human.SetAllowInput(true);
            yield break;
        }

        static public IEnumerator<float> _106PortalTrap(Player player)
        {
            GameObject gameObject = player.GetGameObject() as GameObject;
            Scp106PlayerScript ply106 = gameObject.GetComponent<Scp106PlayerScript>();
            PlyMovementSync pms = gameObject.GetComponent<PlyMovementSync>();

            if(ply106.goingViaThePortal) yield break;

            ply106.goingViaThePortal = true;
            if(AlphaWarheadController.host.doorsClosed)
            {
                if(player.TeamRole.Team != Smod2.API.Team.SCP)
                {
                    player.Damage(500, DamageType.POCKET);
                }
                else
                {
                    List<Player> scp106s = plugin.Server.GetPlayers(Role.SCP_106);
                    if(scp106s.Count > 0)
                    {
                        player.Teleport(scp106s[UnityEngine.Random.Range(0, scp106s.Count)].GetPosition());
                    }
                }
            }
            else
            {
                if(player.TeamRole.Team != Smod2.API.Team.SCP)
                {
                    pms.SetPosition(Vector3.down * 1997f);
                    player.Damage(40, DamageType.SCP_106);
                }
                else
                {
                    List<Player> scp106s = plugin.Server.GetPlayers(Role.SCP_106);
                    if(scp106s.Count > 0)
                    {
                        player.Teleport(scp106s[UnityEngine.Random.Range(0, scp106s.Count)].GetPosition());
                    }
                    else
                    {
                        player.Teleport(SanyaPlugin.Call106PDExit());
                    }
                }
            }
            yield return Timing.WaitForSeconds(5f);
            ply106.goingViaThePortal = false;
            yield break;

            //pms.SetAllowInput(false);
            //for(float i = 0f; i < 50; i++)
            //{
            //    var pos = gameObject.transform.position;
            //    pos.y -= i * 0.01f;
            //    pms.SetPosition(pos);
            //    yield return 0f;
            //}
            //if(AlphaWarheadController.host.doorsClosed)
            //{
            //    if(player.TeamRole.Team != Smod2.API.Team.SCP) player.Damage(500, DamageType.POCKET);
            //}
            //else
            //{
            //    if(player.TeamRole.Team != Smod2.API.Team.SCP) player.Damage(40, DamageType.SCP_106);
            //    pms.SetPosition(Vector3.down * 1997f);
            //}
            //pms.SetAllowInput(true);
        }

        static public IEnumerator<float> _939Boost(Player player)
        {
            int counter = 0;
            Role role = player.TeamRole.Role;
            while(counter < 10)
            {
                SanyaPlugin.Call939CanSee();
                SanyaPlugin.Call939SetSpeedMultiplier(player, 1.5f);
                counter++;
                yield return Timing.WaitForSeconds(1f);
            }
            player.PersonalClearBroadcasts();
            player.PersonalBroadcast(3, $"<size=25>《SCP-939ブーストが終了。》\n </size><size=20>《Ended <SCP-939> boost.》\n</size>", false);
            yield break;
        }

        static public IEnumerator<float> _DOTDamage(Player player, int perDamage, float waitSecond, int maxDamageAmount, DamageType type)
        {
            int curDamageAmount = 0;
            while(curDamageAmount < maxDamageAmount)
            {
                Player target = EventHandler.dot_target.Find(x => x.PlayerId == player.PlayerId);
                if(target == null)
                {
                    break;
                }

                if(player.GetHealth() - perDamage <= 0 || (player.GetGameObject() as GameObject).GetComponent<CharacterClassManager>().curClass == (int)Role.SPECTATOR)
                {
                    player.Damage(perDamage, type);
                    break;
                }
                player.Damage(perDamage, type);
                curDamageAmount += perDamage;
                yield return Timing.WaitForSeconds(waitSecond);
            }

            Player ply = EventHandler.dot_target.Find(x => x.PlayerId == player.PlayerId);
            if(ply != null)
            {
                EventHandler.dot_target.Remove(ply);
            }

            yield break;
        }

        static public IEnumerator<float> _096EnragingIncrase()
        {
            float range = ConfigFile.ServerConfig.GetFloat("096_trigger_range", 20f);
            float angle = ConfigFile.ServerConfig.GetFloat("096_trigger_angle", 70f);
            List<int> ignored = new List<int>(ConfigFile.GetIntList("scp096_ignored_role", false));

            plugin.Debug($"[Coroutine] 096 init (range:{range}/angle:{angle}/ignoreroles:{ignored.Count}/incrasemultiplier:{plugin.scp096_enraged_increase_rage}");
            yield return Timing.WaitForSeconds(0.1f);
            while(Scp096PlayerScript.instance != null && Scp096PlayerScript.instance.iAm096)
            {
                if(Scp096PlayerScript.instance.enraged != Scp096PlayerScript.RageState.Enraged)
                {
                    plugin.Debug($"[Coroutine] 096 break (not enraged)");
                    yield break;
                }

                Transform transform096 = Scp096PlayerScript.instance.camera.transform;
                foreach(var gameObject in PlayerManager.singleton.players)
                {
                    if(gameObject == null
                        || !gameObject.GetComponent<CharacterClassManager>().IsHuman()
                        || gameObject.GetComponent<FlashEffect>().sync_blind
                        || ignored.Contains(gameObject.GetComponent<CharacterClassManager>().curClass)
                        )
                        continue;

                    RaycastHit raycastHit;
                    Transform transform = gameObject.GetComponent<Scp096PlayerScript>().camera.transform;
                    if(Vector3.Angle(transform.forward, (transform096.position - transform.position).normalized) > angle
                        || !Physics.Raycast(transform.transform.position, (transform096.position - transform.position).normalized, out raycastHit, range, Scp096PlayerScript.instance.layerMask)
                        || raycastHit.collider.gameObject.layer != 24
                        || raycastHit.collider.GetComponentInParent<Scp096PlayerScript>() != Scp096PlayerScript.instance
                    )
                        continue;

                    Scp096PlayerScript.instance.rageProgress = Mathf.Clamp01(Scp096PlayerScript.instance.rageProgress + 0.0007f * plugin.scp096_enraged_increase_rage);
                    break;
                }
                yield return 0f;
            }
            plugin.Debug($"[Coroutine] 096 breaked (not 096)");
            yield break;
        }

        [Obsolete("unstable. dont use it.")]
        static public IEnumerator<float> _GrenadeLauncher(Player attacker, Vector positon, Vector forward)
        {
            Vector3 position3 = positon.ToVector3();
            Vector3 forward3 = forward.ToVector3();
            Vector3 targetpoint;
            Vector3 addvector = forward3;
            Vector3 mypos = position3;
            GrenadeManager gm = (attacker.GetGameObject() as GameObject).GetComponent<GrenadeManager>();

            string gid = "SERVER_" + attacker.PlayerId + ":" + (gm.smThrowInteger + 4096);
            gm.CallRpcThrowGrenade(0, attacker.PlayerId, gm.smThrowInteger++ + 4096, new Vector3(0f, 0f, 0f), true, new Vector3(0f, 0f, 0f), false, 0);

            forward3.Scale(new Vector3(0.5f, 0.5f, 0.5f));
            addvector.Scale(new Vector3(0.7f, 0.7f, 0.7f));

            RaycastHit raycastHit;
            if(Physics.Raycast(forward3 + position3, forward3, out raycastHit, 100f, SanyaPlugin.playermask))
            {
                targetpoint = raycastHit.point;

                while(Vector3.Distance(targetpoint, mypos) > 1)
                {
                    mypos += addvector;
                    gm.CallRpcUpdate(gid, mypos, Quaternion.Euler(addvector), Vector3.zero, Vector3.zero);
                    yield return 0f;
                }
                gm.CallRpcExplode(gid, attacker.PlayerId);
                SanyaPlugin.Explode(attacker, new Vector(targetpoint.x, targetpoint.y, targetpoint.z));
            }

            yield break;
        }

        [Obsolete("unstable. dont use it.")]
        static public IEnumerator<float> _JammingName(Player player)
        {
            while(true)
            {
                if(test)
                {
                    NicknameSync nick = (player.GetGameObject() as GameObject).GetComponent<NicknameSync>();

                    nick.NetworkmyNick = System.Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

                    yield return Timing.WaitForSeconds(0.1f);
                }
                else
                {
                    yield break;
                }
            }
        }

        [Obsolete("unstable. dont use it.")]
        static public IEnumerator<float> _SCPRadio(Radio radio)
        {
            while(true)
            {
                if(!SanyaPlugin.test)
                {
                    yield break;
                }
                radio.NetworkisTransmitting = true;
                Radio.roundStarted = false;
                yield return 0f;
            }
        }
    }

    public enum SANYA_AMBIENT_ID
    {
        SCP079 = 6,
        BEEP_1 = 7,
        LURE_DOOR = 20,
        BEEP_2 = 21,
        BEEP_3 = 22,
        BEEP_4 = 22,
        BEEP_5 = 23,
        BEEP_6 = 24,
        BEEP_7 = 25,
        BEEP_8 = 26,
        BEEP_9 = 27,
        BEEP_10 = 29,
        BEEP_11 = 31,
        MOVE = 30
    }

    public enum SANYA_GAME_MODE
    {
        NULL = -1,
        NORMAL = 0,
        NIGHT,
        STORY_173,
        CLASSD_INSURGENCY,
        HCZ,
        STORY_049,
    }

    public class PlayerData
    {
        public PlayerData(string s, bool m, int l, int e) { steamid = s; limited = m; level = l; exp = e; }
        public void AddExp(int amount, Smod2.API.Player target)
        {
            if(string.IsNullOrEmpty(amount.ToString()))
            {
                return;
            }

            if(exp == -1 || level == -1)
            {
                SanyaPlugin.plugin.Debug($"[Redacted] Passed....(Level{level}/Exp{exp})");
                return;
            }

            int sum = exp + amount;

            //1*3 <= 10
            //2*3 <= 7
            //3*3 <= 1
            if(level * 3 <= sum)
            {
                while(level * 3 <= sum)
                {
                    exp = sum - level * 3;
                    sum -= level * 3;
                    target.SendConsoleMessage($"[LevelUp] Level{level} -> {level + 1} (Now:{exp} Next:{Mathf.Clamp((level + 1) * 3 - exp, 0, (level + 1) * 3 - exp)})");
                    level++;
                }
            }
            else
            {
                exp = sum;
            }
        }

        public string steamid;
        public bool limited;
        public int level;
        public int exp;
    }
}
