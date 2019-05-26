using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using MEC;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Config;
using UnityEngine;

namespace SanyaPlugin
{
    [PluginDetails(
    author = "sanyae2439",
    name = "SanyaPlugin",
    description = "nya",
    id = "sanyae2439.sanyaplugin",
    configPrefix = "sanya",
    version = "12.9.5.1",
    SmodMajor = 3,
    SmodMinor = 4,
    SmodRevision = 1
    )]
    public class SanyaPlugin : Plugin
    {
        //LayerMask
        public const int cctvmask = 262144;
        public const int doormask = 134234112;
        public const int playermask = 1208246273;
        public const int teslamask = 4;

        //test
        static public bool test = false;

        //public
        static public System.DateTime roundStartTime;
        static public string scp_override_steamid = "";

        //playersdata
        internal List<PlayerData> playersData;

        //システム系
        [ConfigOption] //サーバー情報送信先IP
        internal string info_sender_to_ip = "hatsunemiku24.ddo.jp";
        [ConfigOption] //サーバー情報送信先ポート
        internal int info_sender_to_port = 37813;
        [ConfigOption] //ログイン時メッセージ
        internal string motd_message = "";
        [ConfigOption] //ログイン時メッセージ（特定role指定）
        internal string motd_target_role = "";
        [ConfigOption] //ログイン時メッセージ（特定role）
        internal string motd_target_message = "";
        [ConfigOption] //ゲームモードランダムの率
        internal int[] event_mode_weight = new int[] { 1, -1, -1, -1, -1, -1 };
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
        [ConfigOption] //リザルト無しでラウンドを終了させる
        internal bool summary_less_mode = false;
        [ConfigOption] //ラウンド終了時に全員を無敵にする
        internal bool endround_all_godmode = false;
        [ConfigOption] //核起動開始時に一部を除きすべてのドアをオープンする
        internal bool nuke_start_countdown_door_lock = false;
        [ConfigOption] //カオスとSCPが同時にいてもラウンド終了しない
        internal bool ci_and_scp_noend = false;

        //Playersデータ&LevelEXP
        [ConfigOption] //playerDataを保存するか
        internal bool data_enabled = false;
        [ConfigOption] //dataはglobalか
        internal bool data_global = true;
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

        //SCP系
        [ConfigOption] //発電機が起動完了した場合開かないように
        internal bool generator_engaged_cantopen = false;
        [ConfigOption] //SCP-079だけになった際発電機ロックがフリー&Tier5に
        internal bool scp079_lone_boost = false;
        [ConfigOption] //SCP-914にプレイヤーが入った際の挙動を少し変更
        internal bool scp914_changing = false;
        [ConfigOption] //ポータル機能の最初のクールタイム
        internal int scp106_portal_to_human_wait = 180;
        [ConfigOption] //SCP-106の囮コンテナに入った際の放送できる時間
        internal int scp106_lure_speaktime = -1;
        [ConfigOption] //SCP-096にダメージを与えた際に発狂開始する
        internal bool scp096_damage_trigger = false;
        [ConfigOption] //SCP-049-2がキルした際もSCP-049が治療可能に
        internal bool infect_by_scp049_2 = false;
        [ConfigOption] //SCP-049が治療できなくなるまでの時間
        internal int infect_limit_time = 4;

        //人間系
        [ConfigOption] //被拘束時にドア/エレベーターを操作不能に
        internal bool handcuffed_cantopen = false;

        //独自要素
        [ConfigOption] //切断したSCPが再接続で戻るように
        internal bool scp_disconnect_at_resetrole = false;
        [ConfigOption] //自殺時に武器を持つのが必要に
        internal bool suicide_need_weapon = false;
        [ConfigOption] //独自自動核
        internal bool original_auto_nuke = false;
        [ConfigOption] //独自核オンの場合の強制セクター2開始ラウンド経過時間
        internal int original_auto_nuke_force_sector2 = -1;
        [ConfigOption] //核起動ボタンの蓋を自動で閉まるように & 核起動室の扉をEXIT_ACC持ち（中尉以上）で開けられるように (-1で無効)
        internal float nuke_button_auto_close = -1f;
        [ConfigOption] //核起爆後は増援が出ないように
        internal bool stop_mtf_after_nuke = false;
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
        [ConfigOption] //クラスDスポーン地点にアイテムを設置（OKになる確率)
        internal int classd_startitem_percent = -1;
        [ConfigOption] //OKの場合に設置するアイテム
        internal int classd_startitem_ok_itemid = 0;
        [ConfigOption] //NGの場合に設置するアイテム
        internal int classd_startitem_no_itemid = -1;

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

        public override void OnDisable()
        {
            Info("さにゃぷらぐいん Disabled");
        }

        public override void OnEnable()
        {
            Info("さにゃぷらぐいん Loaded [Ver" + this.Details.version + "]");
            Info("さにゃぱい");;
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
                if(!Directory.Exists(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin"))
                {
                    Directory.CreateDirectory(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin");
                }
                if(!File.Exists(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json"))
                {
                    File.WriteAllText(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json", "[\n]");
                }
                playersData = JsonConvert.DeserializeObject<List<PlayerData>>(File.ReadAllText(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json"));
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
                if(!Directory.Exists(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin"))
                {
                    Directory.CreateDirectory(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin");
                }
                if(!File.Exists(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json"))
                {
                    File.WriteAllText(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json", "[\n]");
                }
                File.WriteAllText(FileManager.GetAppFolder(GetConfigBool("sanya_data_global")) + "SanyaPlugin" + Path.DirectorySeparatorChar.ToString() + "players.json", JsonConvert.SerializeObject(playersData,Formatting.Indented));
                Info($"playersData Saved.[Count:{playersData.Count}]");
            }
            catch(System.Exception e)
            {
                this.Error($"[DataSaver] {e.Message}");
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

        static public string[] TranslateGeneratorName(RoomType type)
        {
            switch(type)
            {
                case RoomType.ENTRANCE_CHECKPOINT:
                    return new string[] { "上層チェックポイント", "Entrance Checkpoint" };
                case RoomType.HCZ_ARMORY:
                    return new string[] { "中層弾薬庫", "HCZ Armory" };
                case RoomType.SERVER_ROOM:
                    return new string[] { "サーバールーム", "Server Room" };
                case RoomType.MICROHID:
                    return new string[] { "MicroHID室", "MicroHID Room" };
                case RoomType.SCP_049:
                    return new string[] { "SCP-049 エレベーター", "SCP-049 Elevator" };
                case RoomType.SCP_079:
                    return new string[] { "SCP-079 収容室", "SCP-079 Chamber" };
                case RoomType.SCP_096:
                    return new string[] { "SCP-096 収容室", "SCP-096 Chamber" };
                case RoomType.SCP_106:
                    return new string[] { "SCP-106 収容室", "SCP-106 Chamber" };
                case RoomType.SCP_939:
                    return new string[] { "SCP-939 収容室", "SCP-939 Chamber" };
                case RoomType.NUKE:
                    return new string[] { "核格納庫", "Nuke Chamber" };
                default:
                    return new string[] { "不明", "Unknown" };
            }
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

        static public void Call173SnapSound(Player player)
        {
            (player.GetGameObject() as UnityEngine.GameObject).GetComponent<Scp173PlayerScript>().CallRpcSyncAudio();
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
            GrenadeManager gm = (attacker.GetGameObject() as GameObject).GetComponent<GrenadeManager>();
            string gid = "SERVER_" + attacker.PlayerId + ":" + (gm.smThrowInteger + 4096);
            gm.CallRpcThrowGrenade(flashbang ? 1 : 0, attacker.PlayerId, gm.smThrowInteger++ + 4096, new Vector3(0f, 0f, 0f), true, effectonly ? new Vector3(0f, 0f, 0f) : new Vector3(explode_posion.x, explode_posion.y, explode_posion.z), false, 0);
            gm.CallRpcUpdate(gid, new Vector3(explode_posion.x, explode_posion.y, explode_posion.z), Quaternion.Euler(Vector3.zero), Vector3.zero, Vector3.zero);
            gm.CallRpcExplode(gid, attacker.PlayerId);
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

        static public IEnumerator<float> _SummaryLessRoundrestart(SanyaPlugin plugin, int restarttime)
        {
            yield return Timing.WaitForSeconds(restarttime);
            RoundSummary.singleton.CallRpcDimScreen();
            ServerConsole.AddLog("Round restarting");
            plugin.Round.RestartRound();
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

            if(!string.IsNullOrEmpty(hasRank))
            {
                player.SetRank(hasColor, $"Level{data.level} : {hasRank}", null);
            }
            else
            {
                player.SetRank(null, $"Level{data.level}", null);
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

            if(role == Role.SCP_939_53)
            {
                EventHandler.scp939_53_boosting = false;
            }
            else if(role == Role.SCP_939_89)
            {
                EventHandler.scp939_89_boosting = false;
            }
            player.PersonalClearBroadcasts();
            player.PersonalBroadcast(3, $"<size=25>《SCP-939ブーストが終了。》\n </size><size=20>《Ended <SCP-939> boost.》\n</size>", false);
            yield break;
        }

        static public IEnumerator<float> _LureIntercom(Player player, SanyaPlugin plugin)
        {
            yield return Timing.WaitForSeconds(plugin.scp106_lure_speaktime);
            plugin.Info($"[106Lure] Contained({player.Name}):Speaking ended");
            player.SetGodmode(false);
            if(plugin.Server.Map.GetIntercomSpeaker() == null)
            {
                plugin.Server.Map.SetIntercomSpeaker(null);
            }
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
        STORY,
        CLASSD_INSURGENCY,
        HCZ
    }

    public class PlayerData
    {
        public PlayerData(string s,int l,int e) { steamid = s; level = l; exp = e; }
        public void AddExp(int amount,Smod2.API.Player target)
        {
            if(string.IsNullOrEmpty(amount.ToString()))
            {
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
                    target.SendConsoleMessage($"[LevelUp] Level{level} -> {level + 1} (Now:{exp} Next:{Mathf.Clamp((level + 1) * 3 - exp,0, (level + 1) * 3 - exp)})");
                    level++;
                }
            }
            else
            {
                exp = sum;
            }
        }

        public string steamid;
        public int level;
        public int exp;
    }
}
