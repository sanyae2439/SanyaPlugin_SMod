using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;
using Smod2;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
using Smod2.EventSystem.Events;
using Newtonsoft.Json;

namespace SanyaPlugin
{
    class Playerinfo
    {
        public Playerinfo() { }

        public string name { get; set; }

        public string steamid { get; set; }

        public string role { get; set; }
    }

    class Serverinfo
    {
        public Serverinfo()
        {
            players = new List<Playerinfo>();
        }
        public string time { get; set; }

        public string gameversion { get; set; }

        public string smodversion { get; set; }

        public string sanyaversion { get; set; }

        public string name { get; set; }

        public string ip { get; set; }

        public int port { get; set; }

        public int playing { get; set; }

        public int maxplayer { get; set; }

        public int duration { get; set; }

        public List<Playerinfo> players { get; private set; }
    }

    class EventHandler :
        IEventHandlerWaitingForPlayers,
        IEventHandlerRoundStart,
        IEventHandlerRoundEnd,
        IEventHandlerRoundRestart,
        IEventHandlerPlayerJoin,
        IEventHandlerLCZDecontaminate,
        IEventHandlerWarheadStartCountdown,
        IEventHandlerWarheadStopCountdown,
        IEventHandlerWarheadDetonate,
        IEventHandlerSetNTFUnitName,
        IEventHandlerCheckEscape,
        IEventHandlerSetRole,
        IEventHandlerPlayerHurt,
        IEventHandlerPlayerDie,
        IEventHandlerPocketDimensionDie,
        IEventHandlerInfected,
        IEventHandlerRecallZombie,
        IEventHandlerDoorAccess,
        IEventHandlerRadioSwitch,
        IEventHandlerElevatorUse,
        IEventHandlerSCP914Activate,
        IEventHandlerCallCommand,
        IEventHandler106CreatePortal,
        IEventHandler106Teleport,
        IEventHandlerGeneratorAccess,
        IEventHandlerGeneratorUnlock,
        IEventHandlerGeneratorInsertTablet,
        IEventHandlerGeneratorEjectTablet,
        IEventHandlerGeneratorFinish,
        IEventHandler079AddExp,
        IEventHandler079CameraTeleport,
        IEventHandler079Door,
        IEventHandler079Elevator,
        IEventHandler079ElevatorTeleport,
        IEventHandler079LevelUp,
        IEventHandler079Lock,
        IEventHandler079Lockdown,
        IEventHandler079StartSpeaker,
        IEventHandler079StopSpeaker,
        IEventHandler079TeslaGate,
        IEventHandler079UnlockDoors,
        IEventHandlerLure,
        IEventHandlerUpdate
    {
        private SanyaPlugin plugin;
        private Thread infosender;
        private bool running = false;
        private bool sender_live = true;
        UdpClient udpclient = new UdpClient();

        //-----------------var---------------------
        //GlobalStatus
        private bool roundduring = false;
        private bool config_loaded = false;
        private Random rnd = new Random();

        //NightMode
        private List<Room> lcz_lights = new List<Room>();
        private int flickcount = 0;
        private int flickcount_lcz = 0;
        private bool gencomplete = false;

        //EscapeCheck
        private bool isEscaper = false;
        private Vector escape_pos;
        private int escape_player_id;

        //DoorLocker
        private int doorlock_interval = 0;

        //AutoNuker
        //private bool nuke_lock = false;

        //106Lure
        private int lure_id = -1;

        //106Portal
        private Vector portaltemp = new Vector(0, 0, 0);
        private int portal_cooltime = 0;

        //Update
        private int updatecounter = 0;
        private Vector traitor_pos = new Vector(170, 984, 28);

        //Spectator
        private List<Player> playingList = new List<Player>();
        private List<Player> spectatorList = new List<Player>();

        //-----------------------Event---------------------
        public EventHandler(SanyaPlugin plugin)
        {
            this.plugin = plugin;
            running = true;
            infosender = new Thread(new ThreadStart(Sender));
            infosender.Start();
        }

        private void Sender()
        {
            while (running)
            {
                if (config_loaded)
                {
                    try
                    {
                        string ip = plugin.info_sender_to_ip;
                        int port = plugin.info_sender_to_port;

                        if (ip == "none")
                        {
                            plugin.Info("InfoSender to Disabled(config:" + plugin.info_sender_to_ip + ")");
                            running = false;
                            break;
                        }
                        Serverinfo cinfo = new Serverinfo();
                        Server server = this.plugin.Server;

                        DateTime dt = DateTime.Now;
                        cinfo.time = dt.ToString("yyyy-MM-ddTHH:mm:sszzzz");
                        cinfo.gameversion = CustomNetworkManager.CompatibleVersions[0];
                        cinfo.smodversion = PluginManager.GetSmodVersion() + "-" + PluginManager.GetSmodBuild();
                        cinfo.sanyaversion = this.plugin.Details.version;
                        string test = CustomNetworkManager.CompatibleVersions[0];
                        cinfo.name = server.Name;
                        cinfo.ip = server.IpAddress;
                        cinfo.port = server.Port;
                        cinfo.playing = server.NumPlayers - 1;
                        cinfo.maxplayer = server.MaxPlayers;
                        cinfo.duration = server.Round.Duration;

                        cinfo.name = cinfo.name.Replace("$number", (cinfo.port - 7776).ToString());

                        if (cinfo.playing > 0)
                        {
                            foreach (Player player in server.GetPlayers())
                            {
                                Playerinfo ply = new Playerinfo();

                                ply.name = player.Name;
                                ply.steamid = player.SteamId;
                                ply.role = player.TeamRole.Role.ToString();

                                cinfo.players.Add(ply);
                            }
                        }
                        string json = JsonConvert.SerializeObject(cinfo);

                        byte[] sendBytes = Encoding.UTF8.GetBytes(json);

                        udpclient.Send(sendBytes, sendBytes.Length, ip, port);

                        plugin.Debug("[Infosender] " + ip + ":" + port);

                        Thread.Sleep(15000);
                    }
                    catch (Exception e)
                    {
                        if (e.GetType() != typeof(System.Threading.ThreadAbortException))
                        {
                            plugin.Error(e.ToString());
                            sender_live = false;
                        }
                        else
                        {
                            plugin.Debug("[Infosender] Called Abort");
                        }
                    }
                }
            }
        }

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            if (config_loaded)
            {
                plugin.Debug("[InfoSender] Restarting...");
                infosender.Abort();
                infosender = null;

                infosender = new Thread(new ThreadStart(Sender));
                infosender.Start();
                plugin.Debug("[InfoSender] Restart Completed");
            }

            plugin.ReloadConfig();
            config_loaded = true;

            if (config_loaded)
            {
                plugin.Debug("[ConfigLoader] Config Loaded");
            }
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            updatecounter = 0;

            lcz_lights.AddRange(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA));
            lcz_lights = lcz_lights.FindAll(items => { return items.ZoneType == ZoneType.LCZ; });

            roundduring = true;

            plugin.Info("RoundStart!");

            if (plugin.classd_startitem_percent > 0)
            {
                int success_count = 0;
                foreach (Vector spawnpos in plugin.Server.Map.GetSpawnPoints(Role.CLASSD))
                {
                    int percent = rnd.Next(0, 100);
                    ItemType ritem;

                    if (percent <= plugin.classd_startitem_percent)
                    {
                        success_count++;
                        ritem = plugin.classd_startitem_ok_itemid;
                    }
                    else
                    {
                        ritem = plugin.classd_startitem_no_itemid;
                    }

                    if (ritem >= 0)
                    {
                        plugin.Server.Map.SpawnItem((ItemType)ritem, spawnpos, new Vector(0, 0, 0));
                    }
                }
                plugin.Info("Class-D-Contaiment Item Droped! (Success:" + success_count + ")");
            }
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (roundduring)
            {
                plugin.Info("Round Ended [" + ev.Status + "]");
                plugin.Info("Class-D:" + ev.Round.Stats.ClassDAlive + " Scientist:" + ev.Round.Stats.ScientistsAlive + " NTF:" + ev.Round.Stats.NTFAlive + " SCP:" + ev.Round.Stats.SCPAlive + " CI:" + ev.Round.Stats.CiAlive);
            }
            roundduring = false;
        }

        public void OnRoundRestart(RoundRestartEvent ev)
        {
            plugin.Debug("RoundRestart...");
            roundduring = false;
            lcz_lights.Clear();
            gencomplete = false;
            lure_id = -1;
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            plugin.Info("[PlayerJoin] " + ev.Player.Name + "(" + ev.Player.SteamId + ")[" + ev.Player.IpAddress + "]");

            if (plugin.spectator_slot > 0)
            {
                if (playingList.Count >= plugin.Server.MaxPlayers - plugin.spectator_slot)
                {
                    plugin.Info("[Spectator] Join to Spectator : " + ev.Player.Name + "(" + ev.Player.SteamId + ")");
                    ev.Player.OverwatchMode = true;
                    ev.Player.SetRank("nickel", "SPECTATOR", "");
                    spectatorList.Add(ev.Player);
                }
                else
                {
                    plugin.Info("[Spectator] Join to Player : " + ev.Player.Name + "(" + ev.Player.SteamId + ")");
                    ev.Player.OverwatchMode = false;
                    playingList.Add(ev.Player);
                }
            }
        }

        public void OnDecontaminate()
        {
            plugin.Info("LCZ Decontaminated");

            if (plugin.cassie_subtitle)
            {
                plugin.Server.Map.ClearBroadcasts();
                plugin.Server.Map.Broadcast(13, "<size=25>《下層がロックされ、「再収容プロトコル」の準備が出来ました。全ての有機物は破壊されます。》\n </size><size=15>《Light Containment Zone is locked down and ready for decontamination. The removal of organic substances has now begun.》\n</size>", false);
            }
        }

        public void OnDetonate()
        {
            plugin.Info("AlphaWarhead Denotated");
        }

        public void OnStartCountdown(WarheadStartEvent ev)
        {
            if (plugin.cassie_subtitle)
            {
                plugin.Server.Map.ClearBroadcasts();
                if (!ev.IsResumed)
                {
                    plugin.Server.Map.Broadcast(15, "<color=#ff0000><size=23>《「AlphaWarhead」の緊急起爆シーケンスが開始されました。施設の地下区画は、約90秒後に爆破されます。》\n</size><size=15>《Alpha Warhead emergency detonation sequence engaged.The underground section of the facility will be detonated in t-minus 90 seconds.》\n</size></color>", false);
                }
                else
                {
                    double left = ev.TimeLeft;
                    double count = Math.Truncate(left / 10.0) * 10.0;
                    plugin.Server.Map.ClearBroadcasts();
                    plugin.Server.Map.Broadcast(10, "<color=#ff0000><size=25>《緊急起爆シーケンスが再開されました。約" + count.ToString() + "秒後に爆破されます。》\n</size><size=15>《Detonation sequence resumed. t-minus " + count.ToString() + " seconds.》\n</size></color>", false);
                }
            }
        }

        public void OnStopCountdown(WarheadStopEvent ev)
        {
            if (plugin.cassie_subtitle)
            {
                plugin.Server.Map.ClearBroadcasts();
                plugin.Server.Map.Broadcast(7, "<color=#ff0000><size=25>《起爆が取り消されました。システムを再起動します。》\n</size><size=15>《Detonation cancelled. Restarting systems.》\n</size></color>", false);
            }
        }

        public void OnSetNTFUnitName(SetNTFUnitNameEvent ev)
        {
            if (roundduring)
            {
                if (plugin.cassie_subtitle)
                {
                    plugin.Server.Map.ClearBroadcasts();
                    if (plugin.Server.Round.Stats.SCPAlive > 0)
                    {
                        plugin.Server.Map.Broadcast(30, "<color=#6c80ff><size=25>《機動部隊イプシロン-11「" + ev.Unit + "」が施設に到着しました。\n残りの全職員は、機動部隊が貴方の場所へ到着するまで「標準避難プロトコル」の続行を推奨します。\n「" + plugin.Server.Round.Stats.SCPAlive.ToString() + "」オブジェクトが再収容されていません。》\n</size><size=15>《Mobile Task Force Unit, Epsilon-11, designated, '" + ev.Unit + "', has entered the facility.\nAll remaining personnel are advised to proceed with standard evacuation protocols until an MTF squad reaches your destination.\nAwaiting recontainment of: " + plugin.Server.Round.Stats.SCPAlive.ToString() + " SCP subject.》\n</size></color>", false);
                    }
                    else
                    {
                        plugin.Server.Map.Broadcast(30, "<color=#6c80ff><size=25>《機動部隊イプシロン-11「" + ev.Unit + "」が施設に到着しました。\n残りの全職員は、機動部隊が貴方の場所へ到着するまで「標準避難プロトコル」の続行を推奨します。\n重大な脅威が施設内に存在します。注意してください。》\n </size><size=15>《Mobile Task Force Unit, Epsilon-11, designated, '" + ev.Unit + "', has entered the facility.\nAll remaining personnel are advised to proceed with standard evacuation protocols, until MTF squad has reached your destination.\nSubstantial threat to safety is within the facility -- Exercise caution.》\n</size></color>", false);
                    }
                }
            }
        }

        public void OnCheckEscape(PlayerCheckEscapeEvent ev)
        {
            plugin.Debug("[OnCheckEscape] " + ev.Player.Name + ":" + ev.Player.TeamRole.Role);

            isEscaper = true;
            escape_player_id = ev.Player.PlayerId;
            escape_pos = ev.Player.GetPosition();

            plugin.Debug("[OnCheckEscape] escaper x:" + escape_pos.x + " y:" + escape_pos.y + " z:" + escape_pos.z);
        }

        public void OnSetRole(PlayerSetRoleEvent ev)
        {
            plugin.Debug("[OnSetRole] " + ev.Player.Name + ":" + ev.Role);

            //---------EscapeChecker---------
            if (isEscaper)
            {
                if (escape_player_id == ev.Player.PlayerId)
                {
                    if (plugin.escape_spawn && ev.Player.TeamRole.Role != Role.CHAOS_INSURGENCY)
                    {
                        plugin.Debug("[OnSetRole] escaper_id:" + escape_player_id + " / spawn_id:" + ev.Player.PlayerId);
                        plugin.Info("[EscapeChecker] Escape Successfully [" + ev.Player.Name + ":" + ev.Player.TeamRole.Role.ToString() + "]");
                        ev.Player.Teleport(escape_pos, false);
                        isEscaper = false;
                    }
                    else
                    {
                        plugin.Info("[EscapeChecker] Disabled (config or CHAOS) [" + ev.Player.Name + ":" + ev.Player.TeamRole.Role.ToString() + "]");
                        isEscaper = false;
                    }
                }
            }

            //---------------------DefaultAmmo---------------------
            int[] targetammo = new int[] { 0, 0, 0 };
            if (ev.TeamRole.Role == Role.CLASSD)
            {
                targetammo = plugin.default_ammo_classd;
            }
            else if (ev.TeamRole.Role == Role.SCIENTIST)
            {
                targetammo = plugin.default_ammo_scientist;
            }
            else if (ev.TeamRole.Role == Role.FACILITY_GUARD)
            {
                targetammo = plugin.default_ammo_guard;
            }
            else if (ev.TeamRole.Role == Role.CHAOS_INSURGENCY)
            {
                targetammo = plugin.default_ammo_ci;
            }
            else if (ev.TeamRole.Role == Role.NTF_CADET)
            {
                targetammo = plugin.default_ammo_cadet;
            }
            else if (ev.TeamRole.Role == Role.NTF_LIEUTENANT)
            {
                targetammo = plugin.default_ammo_lieutenant;
            }
            else if (ev.TeamRole.Role == Role.NTF_COMMANDER)
            {
                targetammo = plugin.default_ammo_commander;
            }
            else if (ev.TeamRole.Role == Role.NTF_SCIENTIST)
            {
                targetammo = plugin.default_ammo_ntfscientist;
            }
            ev.Player.SetAmmo(AmmoType.DROPPED_5, targetammo[(int)AmmoType.DROPPED_5]);
            ev.Player.SetAmmo(AmmoType.DROPPED_7, targetammo[(int)AmmoType.DROPPED_7]);
            ev.Player.SetAmmo(AmmoType.DROPPED_9, targetammo[(int)AmmoType.DROPPED_9]);
            plugin.Debug("[SetAmmo] " + ev.Player.Name + "(" + ev.TeamRole.Role + ") 5.56mm:" + targetammo[(int)AmmoType.DROPPED_5] + " 7.62mm:" + targetammo[(int)AmmoType.DROPPED_7] + " 9mm:" + targetammo[(int)AmmoType.DROPPED_9]);

        }

        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            plugin.Debug("[OnPlayerHurt] Before " + ev.Attacker.Name + "<" + ev.Attacker.TeamRole.Role + ">(" + ev.DamageType + ":" + ev.Damage + ") -> " + ev.Player.Name + "<" + ev.Player.TeamRole.Role + ">");

            //----------------------------LureSpeak-----------------------
            if (plugin.scp106_lure_speaktime > 0)
            {
                if (ev.DamageType == DamageType.LURE && lure_id == -1)
                {
                    if (plugin.Server.Map.GetIntercomSpeaker() == null)
                    {
                        plugin.Info("[106Lure] Contained(" + ev.Player.Name + "):Start Speaking...(" + plugin.scp106_lure_speaktime + "seconds)");
                        ev.Damage = 0.0f;
                        lure_id = ev.Player.PlayerId;
                        ev.Player.SetGodmode(true);
                        foreach (Smod2.API.Item hasitem in ev.Player.GetInventory())
                        {
                            hasitem.Remove();
                        }
                        ev.Player.PersonalClearBroadcasts();
                        ev.Player.PersonalBroadcast((uint)plugin.scp106_lure_speaktime, "<color=#ffff00><size=25>《あなたはSCP-106の再収容に使用されます。最後に" + plugin.scp106_lure_speaktime + "秒の放送時間が与えられます。》\n</size><size=15>《You will be used for recontainment SCP-106. You can broadcast for " + plugin.scp106_lure_speaktime + " seconds.》\n</size></color>", false);

                        plugin.Server.Map.SetIntercomSpeaker(ev.Player);

                        System.Timers.Timer t = new System.Timers.Timer
                        {
                            Interval = plugin.scp106_lure_speaktime * 1000,
                            AutoReset = false,
                            Enabled = true
                        };
                        t.Elapsed += delegate
                        {
                            plugin.Info("[106Lure] Contained(" + ev.Player.Name + "):Speaking ended");
                            ev.Player.SetGodmode(false);
                            plugin.Server.Map.SetIntercomSpeaker(null);
                            t.Enabled = false;
                        };
                    }
                    else
                    {
                        ev.Damage = 0.0f;
                    }
                }
            }

            if (plugin.friendly_warn)
            {
                if (ev.Attacker != null &&
                    ev.Attacker.Name != "Server" &&
                    ev.Attacker.TeamRole.Team == ev.Player.TeamRole.Team &&
                    ev.Attacker.PlayerId != ev.Player.PlayerId)
                {
                    plugin.Info("[FriendlyFire] " + ev.Attacker.Name + "(" + ev.DamageType.ToString() + ":" + ev.Damage + ") -> " + ev.Player.Name);
                    ev.Attacker.PersonalClearBroadcasts();
                    ev.Attacker.PersonalBroadcast(5, "<color=#ff0000><size=30>《誤射に注意。味方へのダメージは容認されません。(<" + ev.Player.Name + ">への攻撃。)》\n</size><size=20>《Check your fire! Damage to ally forces not be tolerated.(Damaged to <" + ev.Player.Name + ">)》\n</size></color>", false);
                }
            }

            //ダメージ計算開始
            if (ev.DamageType == DamageType.USP)
            {
                if (ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                {
                    ev.Damage *= plugin.usp_damage_multiplier_scp;
                }
                else
                {
                    ev.Damage *= plugin.usp_damage_multiplier_human;
                }
            }
            else if (ev.DamageType == DamageType.FALLDOWN)
            {
                if (ev.Damage <= plugin.fallen_limit)
                {
                    ev.Damage = 0;
                }
            }

            //ロール乗算計算開始
            if (ev.DamageType != DamageType.NUKE && ev.DamageType != DamageType.DECONT && ev.DamageType != DamageType.TESLA) //核と下層ロックとテスラ/HIDは対象外
            {
                if (ev.Player.TeamRole.Role == Role.SCP_173)
                {
                    ev.Damage /= plugin.damage_divisor_scp173;
                }
                else if (ev.Player.TeamRole.Role == Role.SCP_106)
                {
                    ev.Damage /= plugin.damage_divisor_scp106;
                }
                else if (ev.Player.TeamRole.Role == Role.SCP_049)
                {
                    ev.Damage /= plugin.damage_divisor_scp049;
                }
                else if (ev.Player.TeamRole.Role == Role.SCP_049_2)
                {
                    ev.Damage /= plugin.damage_divisor_scp049_2;
                }
                else if (ev.Player.TeamRole.Role == Role.SCP_096)
                {
                    ev.Damage /= plugin.damage_divisor_scp096;
                }
                else if (ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                {
                    ev.Damage /= plugin.damage_divisor_scp939;
                }
            }
            plugin.Debug("[OnPlayerHurt] After " + ev.Attacker.Name + "<" + ev.Attacker.TeamRole.Role + ">(" + ev.DamageType + ":" + ev.Damage + ") -> " + ev.Player.Name + "<" + ev.Player.TeamRole.Role + ">");
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            plugin.Debug("[OnPlayerDie] " + ev.Killer + ":" + ev.DamageTypeVar + " -> " + ev.Player.Name);

            if (plugin.Server.Map.GetIntercomSpeaker() != null)
            {
                if (ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                {
                    plugin.Server.Map.SetIntercomSpeaker(null);
                }
            }

            //----------------------------------------------------キル回復------------------------------------------------  
            //############## SCP-173 ###############
            if (ev.DamageTypeVar == DamageType.SCP_173 &&
                ev.Killer.TeamRole.Role == Role.SCP_173 &&
                plugin.recovery_amount_scp173 > 0)
            {
                if (ev.Killer.GetHealth() + plugin.recovery_amount_scp173 < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(plugin.recovery_amount_scp173);
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }

            //############## SCP-096 ###############
            if (ev.DamageTypeVar == DamageType.SCP_096 &&
                ev.Killer.TeamRole.Role == Role.SCP_096 &&
                plugin.recovery_amount_scp096 > 0)
            {
                if (ev.Killer.GetHealth() + plugin.recovery_amount_scp096 < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(plugin.recovery_amount_scp096);
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }

            //############## SCP-939 ###############
            if (ev.DamageTypeVar == DamageType.SCP_939 &&
                (ev.Killer.TeamRole.Role == Role.SCP_939_53) || (ev.Killer.TeamRole.Role == Role.SCP_939_89) &&
                plugin.recovery_amount_scp939 > 0)
            {
                if (ev.Killer.GetHealth() + plugin.recovery_amount_scp939 < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(plugin.recovery_amount_scp939);
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }

            //############## SCP-049-2 ###############
            if (ev.DamageTypeVar == DamageType.SCP_049_2 &&
                ev.Killer.TeamRole.Role == Role.SCP_049_2 &&
                plugin.recovery_amount_scp049_2 > 0)
            {
                if (ev.Killer.GetHealth() + plugin.recovery_amount_scp049_2 < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(plugin.recovery_amount_scp049_2);
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }
            //----------------------------------------------------キル回復------------------------------------------------

            //----------------------------------------------------ペスト治療------------------------------------------------
            if (ev.DamageTypeVar == DamageType.SCP_049_2 && plugin.infect_by_scp049_2)
            {
                plugin.Info("[Infector] 049-2 Infected!(Limit:" + plugin.infect_limit_time + "s) [" + ev.Killer.Name + "->" + ev.Player.Name + "]");
                ev.DamageTypeVar = DamageType.SCP_049;
                ev.Player.Infect(plugin.infect_limit_time);
            }
            //----------------------------------------------------ペスト治療------------------------------------------------

            //----------------------------------------------------PocketCleaner---------------------------------------------
            if (ev.DamageTypeVar == DamageType.POCKET && plugin.scp106_cleanup)
            {
                plugin.Info("[PocketCleaner] Cleaned (" + ev.Player.Name + ")");
                ev.Player.Teleport(new Vector(0, 0, 0), false);
                ev.SpawnRagdoll = false;
            }
            //----------------------------------------------------PocketCleaner---------------------------------------------
        }

        public void OnLure(PlayerLureEvent ev)
        {
            if (plugin.scp106_lure_speaktime > 0)
            {
                if (plugin.Server.Map.GetIntercomSpeaker() != null)
                {
                    ev.AllowContain = false;
                }
            }
        }

        public void OnPlayerInfected(PlayerInfectedEvent ev)
        {
            plugin.Info("[Infector] 049 Infected!(Limit:" + plugin.infect_limit_time + "s) [" + ev.Attacker.Name + "->" + ev.Player.Name + "]");
            ev.InfectTime = plugin.infect_limit_time;
        }

        public void OnPocketDimensionDie(PlayerPocketDimensionDieEvent ev)
        {
            plugin.Debug("[OnPocketDie] " + ev.Player.Name);

            //------------------------------ディメンション死亡回復(SCP-106)-----------------------------------------
            if (plugin.recovery_amount_scp106 > 0)
            {
                try
                {
                    foreach (Player ply in plugin.Server.GetPlayers())
                    {
                        if (ply.TeamRole.Role == Role.SCP_106)
                        {
                            if (ply.GetHealth() + plugin.recovery_amount_scp106 < ply.TeamRole.MaxHP)
                            {
                                ply.AddHealth(plugin.recovery_amount_scp106);
                            }
                            else
                            {
                                ply.SetHealth(ply.TeamRole.MaxHP);
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public void OnRecallZombie(PlayerRecallZombieEvent ev)
        {
            plugin.Debug("[OnRecallZombie] " + ev.Player.Name + " -> " + ev.Target.Name);
            //----------------------治療回復SCP-049---------------------------------
            if (ev.Player.TeamRole.Role == Role.SCP_049 &&
                plugin.recovery_amount_scp049 > 0)
            {
                if (ev.Player.GetHealth() + plugin.recovery_amount_scp049 < ev.Player.TeamRole.MaxHP)
                {
                    ev.Player.AddHealth(plugin.recovery_amount_scp049);
                }
                else
                {
                    ev.Player.SetHealth(ev.Player.TeamRole.MaxHP);
                }
            }
        }

        public void OnDoorAccess(PlayerDoorAccessEvent ev)
        {
            plugin.Debug("[OnDoorAccess] " + ev.Door.Name + "(" + ev.Door.Open + "):" + ev.Door.Permission + "=" + ev.Allow);

            if ((int)plugin.doorlock_itemid > -1)
            {
                if (doorlock_interval >= plugin.doorlock_interval_second)
                {
                    if (ev.Player.TeamRole.Role == Role.NTF_COMMANDER)
                    {
                        if (ev.Player.GetCurrentItemIndex() > -1)
                        {
                            if (ev.Player.GetCurrentItem().ItemType == plugin.doorlock_itemid)
                            {
                                if (!ev.Door.Name.Contains("CHECKPOINT") && !ev.Door.Name.Contains("GATE_"))
                                {
                                    plugin.Info("[DoorLock] " + ev.Player.Name + " lock " + ev.Door.Name);
                                    doorlock_interval = 0;
                                    ev.Door.Locked = true;
                                    ev.Door.Open = false;
                                    System.Timers.Timer t = new System.Timers.Timer
                                    {
                                        Interval = plugin.doorlock_locked_second * 1000,
                                        AutoReset = false,
                                        Enabled = true
                                    };
                                    t.Elapsed += delegate
                                    {
                                        ev.Allow = true;
                                        ev.Door.Locked = false;
                                        t.Enabled = false;
                                    };
                                }
                            }
                        }
                    }
                }
            }

            if (plugin.intercom_information)
            {
                if (ev.Door.Name == "INTERCOM")
                {
                    ev.Allow = true;
                }
            }

            if (plugin.handcuffed_cantopen)
            {
                if (ev.Player.IsHandcuffed())
                {
                    ev.Allow = false;
                }
            }
        }

        public void OnPlayerRadioSwitch(PlayerRadioSwitchEvent ev)
        {
            plugin.Debug("[OnPlayerRadioSwitch] " + ev.Player.Name + ":" + ev.ChangeTo);

            if (plugin.radio_enhance)
            {
                if (ev.ChangeTo == RadioStatus.ULTRA_RANGE)
                {
                    if (null == plugin.Server.Map.GetIntercomSpeaker())
                    {
                        plugin.Server.Map.SetIntercomSpeaker(ev.Player);
                    }
                }
                else
                {
                    if (plugin.Server.Map.GetIntercomSpeaker() != null)
                    {
                        if (ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                        {
                            plugin.Server.Map.SetIntercomSpeaker(null);
                        }
                    }
                }
            }

        }

        public void OnElevatorUse(PlayerElevatorUseEvent ev)
        {
            plugin.Debug("[OnElevatorUse] " + ev.Elevator.ElevatorType + "[" + ev.Elevator.ElevatorStatus + "] => " + ev.Elevator.MovingSpeed);

            if (plugin.handcuffed_cantopen)
            {
                if (ev.Player.IsHandcuffed())
                {
                    ev.AllowUse = false;
                }
            }
        }

        public void On106CreatePortal(Player106CreatePortalEvent ev)
        {
            plugin.Debug("[On106CreatePortal] " + ev.Player.Name + "[" + ev.Player.Get106Portal().x + "." + ev.Player.Get106Portal().y + "." + ev.Player.Get106Portal().z + ".](" + ev.Position.x + "." + ev.Position.y + "." + ev.Position.z + ")");

            if (plugin.scp106_portal_to_human)
            {
                if ((ev.Position.x == portaltemp.x &&
                ev.Position.y == portaltemp.y &&
                ev.Position.z == portaltemp.z) &&
              !(ev.Position.x == ev.Player.Get106Portal().x &&
                ev.Position.y == ev.Player.Get106Portal().y &&
                ev.Position.z == ev.Player.Get106Portal().z))
                {
                    plugin.Debug("through");
                }
                else
                {
                    if (portal_cooltime >= 3)
                    {
                        if (ev.Position.x == ev.Player.Get106Portal().x &&
                           ev.Position.y == ev.Player.Get106Portal().y &&
                           ev.Position.z == ev.Player.Get106Portal().z)
                        {
                            List<Player> humanlist = new List<Player>();
                            foreach (Player item in plugin.Server.GetPlayers())
                            {
                                if (item.TeamRole.Team != Smod2.API.Team.SCP &&
                                    item.TeamRole.Team != Smod2.API.Team.SPECTATOR)
                                {
                                    if (!(item.GetPosition().y < -1900 && item.GetPosition().y > -2000))
                                    {
                                        humanlist.Add(item);
                                    }
                                }
                            }

                            if (humanlist.Count <= 0)
                            {
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(5, "<size=25>《ターゲットが見つからないようだ。》\n </size><size=15>《Target not found.》\n</size>", false);
                                plugin.Info("[106Portal] No Humans");
                            }
                            else
                            {
                                int rndresult = rnd.Next(0, humanlist.Count);
                                Vector temppos = new Vector(humanlist[rndresult].GetPosition().x, humanlist[rndresult].GetPosition().y - 2.3f, humanlist[rndresult].GetPosition().z);
                                ev.Position = temppos;
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(5, "<size=25>《生存者<" + humanlist[rndresult].Name + ">の近くにポータルを作成します。》\n </size><size=15>《Create portal at close to <" + humanlist[rndresult].Name + ">.》\n</size>", false);
                                plugin.Info("[106Portal] Target:" + humanlist[rndresult].Name);
                            }
                        }
                        portaltemp = ev.Position;
                        portal_cooltime = 0;
                    }
                }
            }
        }

        public void On106Teleport(Player106TeleportEvent ev)
        {
            plugin.Debug("[On106Teleport] " + ev.Player.Name + "[" + ev.Player.Get106Portal().x + "." + ev.Player.Get106Portal().y + "." + ev.Player.Get106Portal().z + ".](" + ev.Position.x + "." + ev.Position.y + "." + ev.Position.z + ")");
        }

        public void OnSCP914Activate(SCP914ActivateEvent ev)
        {
            bool isAfterChange = false;
            Vector newpos = new Vector(ev.OutputPos.x, ev.OutputPos.y + 1.0f, ev.OutputPos.z);

            plugin.Debug("[OnSCP914Activate] " + ev.User.Name);

            if (plugin.scp914_changing)
            {

                foreach (UnityEngine.Collider item in ev.Inputs)
                {
                    Pickup comp1 = item.GetComponent<Pickup>();
                    if (comp1 == null)
                    {
                        NicknameSync comp2 = item.GetComponent<NicknameSync>();
                        CharacterClassManager comp3 = item.GetComponent<CharacterClassManager>();
                        PlyMovementSync comp4 = item.GetComponent<PlyMovementSync>();
                        PlayerStats comp5 = item.GetComponent<PlayerStats>();

                        if (comp2 != null && comp3 != null && comp4 != null && comp5 != null && item.gameObject != null)
                        {
                            UnityEngine.GameObject gameObject = item.gameObject;
                            ServerMod2.API.SmodPlayer player = new ServerMod2.API.SmodPlayer(gameObject);

                            if (ev.KnobSetting == KnobSetting.COARSE)
                            {
                                plugin.Info("[SCP914] COARSE:" + player.Name);

                                foreach (Smod2.API.Item hasitem in player.GetInventory())
                                {
                                    hasitem.Remove();
                                }
                                player.SetAmmo(AmmoType.DROPPED_5, 0);
                                player.SetAmmo(AmmoType.DROPPED_7, 0);
                                player.SetAmmo(AmmoType.DROPPED_9, 0);

                                player.ThrowGrenade(ItemType.FRAG_GRENADE, true, new Vector(0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FRAG_GRENADE, true, new Vector(0, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FRAG_GRENADE, true, new Vector(-0.25f, 1, 0), true, newpos, true, 1.0f);

                                player.Kill(DamageType.RAGDOLLLESS);
                            }
                            else if (ev.KnobSetting == KnobSetting.ONE_TO_ONE && !isAfterChange)
                            {
                                plugin.Info("[SCP914] 1to1:" + player.Name);

                                List<Player> speclist = new List<Player>();
                                foreach (Player spec in plugin.Server.GetPlayers())
                                {
                                    if (spec.TeamRole.Role == Role.SPECTATOR)
                                    {
                                        speclist.Add(spec);
                                    }
                                }

                                plugin.Debug(speclist.Count.ToString());

                                if (speclist.Count > 0)
                                {
                                    int targetspec = rnd.Next(0, speclist.Count - 1);

                                    speclist[targetspec].ChangeRole(player.TeamRole.Role, true, false, true, true);
                                    speclist[targetspec].Teleport(newpos, false);
                                    player.ChangeRole(Role.SPECTATOR, true, false, false, false);
                                    plugin.Info("[SCP914] 1to1_Changed_to:" + speclist[targetspec].Name);
                                }
                                isAfterChange = true;
                            }
                            else if (ev.KnobSetting == KnobSetting.FINE)
                            {
                                plugin.Info("[SCP914] FINE:" + player.Name);

                                foreach (Smod2.API.Item hasitem in player.GetInventory())
                                {
                                    hasitem.Remove();
                                }
                                player.SetAmmo(AmmoType.DROPPED_5, 0);
                                player.SetAmmo(AmmoType.DROPPED_7, 0);
                                player.SetAmmo(AmmoType.DROPPED_9, 0);

                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(0, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(-0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.Kill(DamageType.RAGDOLLLESS);

                                plugin.Server.Map.SpawnItem((ItemType)rnd.Next(0, 30), newpos, new Vector(0, 0, 0));
                            }
                            else if (ev.KnobSetting == KnobSetting.VERY_FINE)
                            {
                                plugin.Info("[SCP914] VERY_FINE:" + player.Name);

                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(0, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(-0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ChangeRole(Role.SCP_049_2, true, false, true, false);

                                System.Timers.Timer t = new System.Timers.Timer
                                {
                                    Interval = 500,
                                    AutoReset = true,
                                    Enabled = true
                                };
                                t.Elapsed += delegate
                                {
                                    player.SetHealth(player.GetHealth() - 10, DamageType.DECONT);
                                    if (player.GetHealth() - 10 <= 0)
                                    {
                                        player.SetHealth(player.GetHealth() - 10, DamageType.DECONT);
                                        t.AutoReset = false;
                                        t.Enabled = false;
                                    }
                                };
                            }

                        }
                    }
                }
                isAfterChange = true;
            }
        }

        public void OnCallCommand(PlayerCallCommandEvent ev)
        {
            plugin.Debug("[OnCallCommand] [Before] Called:" + ev.Player.Name + " Command:" + ev.Command + " Return:" + ev.ReturnMessage);

            if (ev.ReturnMessage == "Command not found.")
            {
                if (ev.Command.StartsWith("kill"))
                {
                    plugin.Info("[SelfKiller] " + ev.Player.Name);
                    if (ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR && ev.Player.TeamRole.Team != Smod2.API.Team.NONE)
                    {
                        ev.ReturnMessage = "Success.";
                        ev.Player.PersonalClearBroadcasts();
                        ev.Player.PersonalBroadcast(5, "<size=25>《あなたは自殺しました。》\n </size><size=15>《You suicided.》\n</size>", false);
                        ev.Player.SetGodmode(false);
                        ev.Player.Kill(DamageType.DECONT);
                    }
                    else
                    {
                        ev.ReturnMessage = "あなたは観戦状態です。";
                    }
                }
                else if (ev.Command.StartsWith("sinfo"))
                {
                    if (ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                    {
                        plugin.Info("[SCPInfo] " + ev.Player.Name);

                        string scplist = "仲間のSCP情報：\n";
                        foreach (Player items in plugin.Server.GetPlayers().FindAll(fl => { return fl.TeamRole.Team == Smod2.API.Team.SCP; }))
                        {
                            scplist += items.Name + " : " + items.TeamRole.Role + "(" + items.GetHealth() + "HP)\n";
                        }

                        ev.ReturnMessage = scplist;
                        ev.Player.PersonalClearBroadcasts();
                        ev.Player.PersonalBroadcast(10, "<size=25>" + scplist + "</size>", false);
                    }
                    else
                    {
                        ev.ReturnMessage = "あなたはSCP陣営ではありません。";
                    }
                }
                else if (ev.Command.StartsWith("939sp"))
                {
                    if (ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                    {
                        plugin.Info("[939Speaker] " + ev.Player.Name);

                        if (null == plugin.Server.Map.GetIntercomSpeaker())
                        {
                            plugin.Server.Map.SetIntercomSpeaker(ev.Player);
                            ev.Player.PersonalClearBroadcasts();
                            ev.Player.PersonalBroadcast(5, "<size=25>《放送を開始します。》\n </size><size=15>《You will broadcast.》\n</size>", false);
                            ev.ReturnMessage = "放送を開始します。";
                        }
                        else
                        {
                            if (ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                            {
                                plugin.Server.Map.SetIntercomSpeaker(null);
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(5, "<size=25>《放送を終了しました。》\n </size><size=15>《You finished broadcasting.》\n</size>", false);
                                ev.ReturnMessage = "放送を終了しました。";
                            }
                            else
                            {
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(5, "<size=25>《誰かが放送中です。》\n </size><size=15>《Someone is broadcasting.》\n</size>", false);
                                ev.ReturnMessage = "誰かが放送中です。";
                            }
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "あなたはSCP-939ではありません。";
                    }
                }
            }

            plugin.Debug("[OnCallCommand] Called:" + ev.Player.Name + " Command:" + ev.Command + " Return:" + ev.ReturnMessage);
        }

        public void OnUpdate(UpdateEvent ev)
        {
            if (updatecounter % 60 == 0 && config_loaded)
            {
                updatecounter = 0;

                if (plugin.spectator_slot > 0)
                {
                    plugin.Debug("[SpectatorCheck] playing:" + playingList.Count + " spectator:" + spectatorList.Count);

                    List<Player> players = plugin.Server.GetPlayers();

                    foreach (Player ply in playingList.ToArray())
                    {
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug("[SpectatorCheck] delete player:" + ply.SteamId);
                            playingList.Remove(ply);
                        }
                    }

                    foreach (Player ply in spectatorList.ToArray())
                    {
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug("[SpectatorCheck] delete spectator:" + ply.SteamId);
                            spectatorList.Remove(ply);
                        }
                    }

                    if (playingList.Count < plugin.Server.MaxPlayers - plugin.spectator_slot)
                    {
                        if (spectatorList.Count > 0)
                        {
                            plugin.Info("[SpectatorCheck] Spectator to Player:" + spectatorList[0].Name + "(" + spectatorList[0].SteamId + ")");
                            spectatorList[0].OverwatchMode = false;
                            spectatorList[0].SetRank();
                            spectatorList[0].HideTag(true);
                            playingList.Add(spectatorList[0]);
                            spectatorList.RemoveAt(0);
                        }
                    }

                }

                if (plugin.night_mode)
                {
                    if (roundduring && !gencomplete)
                    {
                        if (flickcount >= 10)
                        {
                            plugin.Debug("HCZ Flick");
                            Generator079.mainGenerator.CallRpcOvercharge();
                            flickcount = 0;
                        }
                        else
                        {
                            flickcount++;
                        }

                        if (flickcount_lcz >= 8)
                        {
                            plugin.Debug("LCZ Flick");
                            lcz_lights.ForEach(items => { items.FlickerLights(); });
                            flickcount_lcz = 0;
                        }
                        else
                        {
                            flickcount_lcz++;
                        }
                    }
                }

                if (plugin.scp106_portal_to_human)
                {
                    if (portal_cooltime < 3)
                    {
                        portal_cooltime++;
                    }
                }

                if (plugin.doorlock_locked_second > 0)
                {
                    if (doorlock_interval < plugin.doorlock_interval_second)
                    {
                        doorlock_interval++;
                    }
                }

                if (plugin.intercom_information)
                {
                    plugin.pluginManager.Server.Map.SetIntercomContent(IntercomStatus.Ready, "READY\nSCP LEFT:" + plugin.pluginManager.Server.Round.Stats.SCPAlive + "\nCLASS-D LEFT:" + plugin.pluginManager.Server.Round.Stats.ClassDAlive + "\nSCIENTIST LEFT:" + plugin.pluginManager.Server.Round.Stats.ScientistsAlive);
                }

                if (plugin.title_timer)
                {
                    string title = ConfigManager.Manager.Config.GetStringValue("player_list_title", "Unnamed Server") + " RoundTime: " + plugin.pluginManager.Server.Round.Duration / 60 + ":" + plugin.pluginManager.Server.Round.Duration % 60;
                    plugin.pluginManager.Server.PlayerListTitle = title;
                }

                if (plugin.traitor_limitter > 0)
                {
                    try
                    {
                        foreach (Player ply in plugin.pluginManager.Server.GetPlayers())
                        {
                            if ((ply.TeamRole.Role == Role.NTF_CADET ||
                                ply.TeamRole.Role == Role.NTF_LIEUTENANT ||
                                ply.TeamRole.Role == Role.NTF_SCIENTIST ||
                                ply.TeamRole.Role == Role.NTF_COMMANDER ||
                                ply.TeamRole.Role == Role.FACILITY_GUARD ||
                                ply.TeamRole.Role == Role.CHAOS_INSURGENCY) && ply.IsHandcuffed())
                            {
                                Vector pos = ply.GetPosition();
                                int ntfcount = plugin.pluginManager.Server.Round.Stats.NTFAlive;
                                int cicount = plugin.pluginManager.Server.Round.Stats.CiAlive;

                                plugin.Debug("[TraitorCheck] NTF:" + ntfcount + " CI:" + cicount + " LIMIT:" + plugin.traitor_limitter);
                                plugin.Debug("[TraitorCheck] " + ply.Name + "(" + ply.TeamRole.Role.ToString() + ") x:" + pos.x + " y:" + pos.y + " z:" + pos.z + " cuff:" + ply.IsHandcuffed());

                                if ((pos.x >= 172 && pos.x <= 182) &&
                                    (pos.y >= 980 && pos.y <= 990) &&
                                    (pos.z >= 25 && pos.z <= 34))
                                {
                                    if ((ply.TeamRole.Role == Role.CHAOS_INSURGENCY && cicount <= plugin.traitor_limitter) ||
                                        (ply.TeamRole.Role != Role.CHAOS_INSURGENCY && ntfcount <= plugin.traitor_limitter))
                                    {
                                        int rndresult = rnd.Next(0, 100);
                                        plugin.Debug("[TraitorCheck] Traitoring... [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + "<=" + plugin.traitor_chance_percent + "]");
                                        if (rndresult <= plugin.traitor_chance_percent)
                                        {
                                            if (ply.TeamRole.Role == Role.CHAOS_INSURGENCY)
                                            {
                                                ply.ChangeRole(Role.NTF_CADET, true, false, true, true);
                                                ply.Teleport(traitor_pos, true);
                                            }
                                            else
                                            {
                                                ply.ChangeRole(Role.CHAOS_INSURGENCY, true, false, true, true);
                                                ply.Teleport(traitor_pos, true);
                                            }
                                            plugin.Info("[TraitorCheck] Success [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + "<=" + plugin.traitor_chance_percent + "]");
                                        }
                                        else
                                        {
                                            ply.Teleport(traitor_pos, true);
                                            ply.Kill(DamageType.TESLA);
                                            plugin.Info("[TraitorCheck] Failed [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + ">=" + plugin.traitor_chance_percent + "]");
                                        }
                                    };
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        plugin.Error(e.Message);
                    }
                }

                if (!sender_live)
                {
                    plugin.Warn("[InfoSender] Crashed-Rebooting...");
                    infosender.Abort();
                    infosender = null;

                    infosender = new Thread(new ThreadStart(Sender));
                    infosender.Start();
                    sender_live = true;
                    plugin.Warn("[InfoSender] Rebooting Completed");
                }
            }

            updatecounter++;
        }

        public void OnGeneratorAccess(PlayerGeneratorAccessEvent ev)
        {
            plugin.Debug("[OnGeneratorAccess] " + ev.Player.Name + ":" + ev.Generator.Room.RoomType.ToString() + ":" + ev.Allow);
            plugin.Debug(ev.Generator.Locked + ":" + ev.Generator.Open + ":" + ev.Generator.HasTablet + ":" + ev.Generator.TimeLeft + ":" + ev.Generator.Engaged);

            if (plugin.generators_fix)
            {
                if (!ev.Generator.Engaged)
                {
                    if (ev.Generator.Room.RoomType != RoomType.HCZ_ARMORY)
                    {
                        if (!ev.Player.GetBypassMode())
                        {
                            ev.Allow = false;
                        }
                    }
                }
                else
                {
                    if (!ev.Player.GetBypassMode())
                    {
                        ev.Allow = false;
                    }
                }
            }
        }

        public void OnGeneratorUnlock(PlayerGeneratorUnlockEvent ev)
        {
            plugin.Debug("[OnGeneratorUnlock] " + ev.Player.Name + ":" + ev.Generator.Room.RoomType.ToString() + ":" + ev.Allow);
            plugin.Debug(ev.Generator.Locked + ":" + ev.Generator.Open + ":" + ev.Generator.HasTablet + ":" + ev.Generator.TimeLeft + ":" + ev.Generator.Engaged);

            if (plugin.generators_fix)
            {
                if (ev.Allow)
                {
                    ev.Generator.Open = true;
                }
            }
        }

        public void OnGeneratorInsertTablet(PlayerGeneratorInsertTabletEvent ev)
        {
            plugin.Debug("[OnGeneratorInsertTablet] " + ev.Player.Name + ":" + ev.Generator.Room.RoomType.ToString() + ":" + ev.Allow + "(" + ev.RemoveTablet + ")");
            plugin.Debug(ev.Generator.Locked + ":" + ev.Generator.Open + ":" + ev.Generator.HasTablet + ":" + ev.Generator.TimeLeft + ":" + ev.Generator.Engaged);

            if (plugin.cassie_subtitle)
            {
                if (ev.Allow)
                {
                    string genName = "", genNameEn = "";

                    if (ev.Generator.Room.RoomType == RoomType.ENTRANCE_CHECKPOINT)
                    {
                        genName = "上層チェックポイント";
                        genNameEn = "Entrance Checkpoint";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.HCZ_ARMORY)
                    {
                        genName = "中層弾薬庫";
                        genNameEn = "HCZ Armory";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.SERVER_ROOM)
                    {
                        genName = "サーバールーム";
                        genNameEn = "Server Room";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.MICROHID)
                    {
                        genName = "MicroHID室";
                        genNameEn = "MicroHID Room";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.SCP_049)
                    {
                        genName = "SCP-049 エレベーター前";
                        genNameEn = "Front of the SCP-049 elevator";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.SCP_079)
                    {
                        genName = "SCP-079 収容室前";
                        genNameEn = "Front of the SCP-049 chamber";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.SCP_096)
                    {
                        genName = "SCP-096 収容室前";
                        genNameEn = "Front of the SCP-096 chamber";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.SCP_106)
                    {
                        genName = "SCP-106 収容室";
                        genNameEn = "SCP-106 chamber";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.SCP_939)
                    {
                        genName = "SCP-939 収容室";
                        genNameEn = "SCP-939 chamber";
                    }
                    else if (ev.Generator.Room.RoomType == RoomType.NUKE)
                    {
                        genName = "核格納庫";
                        genNameEn = "Nuke chamber";
                    }
                    else
                    {
                        genName = "不明";
                        genNameEn = "Unknown";
                    }

                    plugin.Server.Map.ClearBroadcasts();
                    plugin.Server.Map.Broadcast(10, "<color=#bbee00><size=25>《<" + genName + ">の発電機が起動を始めました。》\n</size><size=15>《Generator<" + genNameEn + "> has starting.》\n</size></color>", false);
                }
            }
        }

        public void OnGeneratorEjectTablet(PlayerGeneratorEjectTabletEvent ev)
        {
            plugin.Debug("[OnGeneratorEjectTablet] " + ev.Player.Name + ":" + ev.Generator.Room.RoomType.ToString() + ":" + ev.Allow + "(" + ev.SpawnTablet + ")");
            plugin.Debug(ev.Generator.Locked + ":" + ev.Generator.Open + ":" + ev.Generator.HasTablet + ":" + ev.Generator.TimeLeft + ":" + ev.Generator.Engaged);
        }

        public void OnGeneratorFinish(GeneratorFinishEvent ev)
        {
            plugin.Debug("[OnGeneratorFinish] " + ev.Generator.Room.RoomType.ToString());
            plugin.Debug(ev.Generator.Locked + ":" + ev.Generator.Open + ":" + ev.Generator.HasTablet + ":" + ev.Generator.TimeLeft + ":" + ev.Generator.Engaged);

            int engcount = 1;
            foreach (Generator gens in plugin.Server.Map.GetGenerators())
            {
                if (gens.Engaged)
                {
                    engcount++;
                }
            }

            if (engcount >= 5)
            {
                gencomplete = true;
            }

            if (plugin.generators_fix)
            {
                ev.Generator.Open = false;
            }

            if (plugin.cassie_subtitle)
            {
                string genName = "", genNameEn = "";

                if (ev.Generator.Room.RoomType == RoomType.ENTRANCE_CHECKPOINT)
                {
                    genName = "上層チェックポイント";
                    genNameEn = "Entrance Checkpoint";
                }
                else if (ev.Generator.Room.RoomType == RoomType.HCZ_ARMORY)
                {
                    genName = "中層弾薬庫";
                    genNameEn = "HCZ Armory";
                }
                else if (ev.Generator.Room.RoomType == RoomType.SERVER_ROOM)
                {
                    genName = "サーバールーム";
                    genNameEn = "Server Room";
                }
                else if (ev.Generator.Room.RoomType == RoomType.MICROHID)
                {
                    genName = "MicroHID室";
                    genNameEn = "MicroHID Room";
                }
                else if (ev.Generator.Room.RoomType == RoomType.SCP_049)
                {
                    genName = "SCP-049 エレベーター前";
                    genNameEn = "Front of the SCP-049 elevator";
                }
                else if (ev.Generator.Room.RoomType == RoomType.SCP_079)
                {
                    genName = "SCP-079 収容室前";
                    genNameEn = "Front of the SCP-049 chamber";
                }
                else if (ev.Generator.Room.RoomType == RoomType.SCP_096)
                {
                    genName = "SCP-096 収容室前";
                    genNameEn = "Front of the SCP-096 chamber";
                }
                else if (ev.Generator.Room.RoomType == RoomType.SCP_106)
                {
                    genName = "SCP-106 収容室";
                    genNameEn = "SCP-106 chamber";
                }
                else if (ev.Generator.Room.RoomType == RoomType.SCP_939)
                {
                    genName = "SCP-939 収容室";
                    genNameEn = "SCP-939 chamber";
                }
                else if (ev.Generator.Room.RoomType == RoomType.NUKE)
                {
                    genName = "核格納庫";
                    genNameEn = "Nuke chamber";
                }
                else
                {
                    genName = "不明";
                    genNameEn = "Unknown";
                }

                if (!gencomplete)
                {
                    plugin.Server.Map.ClearBroadcasts();
                    plugin.Server.Map.Broadcast(10, "<color=#bbee00><size=25>《5つ中" + engcount + "つ目の発電機<" + genName + ">の起動が完了しました。》\n</size><size=15>《" + engcount + " out of 5 generators activated. <" + genNameEn + ">》\n</size></color>", false);
                }
                else
                {
                    plugin.Server.Map.ClearBroadcasts();
                    plugin.Server.Map.Broadcast(20, "<color=#bbee00><size=25>《5つ中" + engcount + "つ目の発電機<" + genName + ">の起動が完了しました。\n全ての発電機が起動されました。最後再収容手順を開始します。\n中層は約一分後に過充電されます。》\n</size><size=15>《" + engcount + " out of 5 generators activated. <" + genNameEn + ">\nAll generators has been sucessfully engaged.\nFinalizing recontainment sequence.\nHeavy containment zone will overcharge in t-minus 1 minutes.》\n </size></color>", false);
                }
            }
        }

        public void On079AddExp(Player079AddExpEvent ev)
        {
            plugin.Debug("[On079AddExp] " + ev.Player.Name + ":" + ev.ExpToAdd + "(" + ev.ExperienceType + ")");
        }

        public void On079CameraTeleport(Player079CameraTeleportEvent ev)
        {
            plugin.Debug("[On079CameraTeleport] " + ev.Player.Name + ":" + ev.Camera.x + "/" + ev.Camera.y + "/" + ev.Camera.z + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079Door(Player079DoorEvent ev)
        {
            plugin.Debug("[On079Door] " + ev.Player.Name + ":" + ev.Door.Name + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079Elevator(Player079ElevatorEvent ev)
        {
            plugin.Debug("[On079Elevator] " + ev.Player.Name + ":" + ev.Elevator.ElevatorType + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079ElevatorTeleport(Player079ElevatorTeleportEvent ev)
        {
            plugin.Debug("[On079ElevatorTeleport] " + ev.Player.Name + ":" + ev.Elevator.ElevatorType + "/" + ev.Camera.x + "/" + ev.Camera.y + "/" + ev.Camera.z + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079LevelUp(Player079LevelUpEvent ev)
        {
            plugin.Debug("[On079LevelUp] " + ev.Player.Name);
        }

        public void On079Lock(Player079LockEvent ev)
        {
            plugin.Debug("[On079Lock] " + ev.Player.Name + ":" + ev.Door.Name + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079Lockdown(Player079LockdownEvent ev)
        {
            plugin.Debug("[On079Lockdown] " + ev.Player.Name + ":" + ev.Room.RoomType + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079StartSpeaker(Player079StartSpeakerEvent ev)
        {
            ev.APDrain = 0.0f;
            ev.Player.Scp079Data.SpeakerAPPerSecond = 0.0f;
            plugin.Debug("[On079StartSpeaker] " + ev.Player.Name + ":" + ev.Room.RoomType + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079StopSpeaker(Player079StopSpeakerEvent ev)
        {
            plugin.Debug("[On079StopSpeaker] " + ev.Player.Name + ":" + ev.Room.RoomType + ":" + ev.Allow);
        }

        public void On079TeslaGate(Player079TeslaGateEvent ev)
        {
            plugin.Debug("[On079TeslaGate] " + ev.Player.Name + ":" + ev.TeslaGate.Position.x + "/" + ev.TeslaGate.Position.y + "/" + ev.TeslaGate.Position.z + "(" + ev.APDrain + "):" + ev.Allow);
        }

        public void On079UnlockDoors(Player079UnlockDoorsEvent ev)
        {
            plugin.Debug("[On079UnlockDoors] " + ev.Player.Name + ":" + ev.Allow);
        }
    }
}
