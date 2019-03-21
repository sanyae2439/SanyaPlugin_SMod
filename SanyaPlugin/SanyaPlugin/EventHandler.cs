using System;
using System.Linq;
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
using UnityEngine;
using RemoteAdmin;

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
        IEventHandlerCheckRoundEnd,
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
        IEventHandlerGeneratorAccess,
        IEventHandlerGeneratorUnlock,
        IEventHandlerGeneratorInsertTablet,
        IEventHandlerGeneratorFinish,
        IEventHandler079Door,
        IEventHandler079StartSpeaker,
        IEventHandler079Lockdown,
        IEventHandlerLure,
        IEventHandlerSummonVehicle,
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
        private System.Random rnd = new System.Random();
        private bool friendlyfire = false;
        private bool debug = false;

        //Eventer
        private SANYA_GAME_MODE eventmode = SANYA_GAME_MODE.NULL;
        private List<Room> hcz_hallways = new List<Room>();
        private bool storyshaker = false;
        private int startwait = -1;
        private int chamber173count = 0;
        private int hall173count = 0;
        private Vector armory = null;
        private Vector cam173hall = null;
        private Vector cam173chamber = null;

        //NightMode
        private List<Room> lcz_lights = new List<Room>();
        private int flickcount = 0;
        private int flickcount_lcz = 0;
        private bool gencomplete = false;

        //InventoryActer
        private Item[] itemtable = { };
        private List<string> genperm;

        //SummaryLess
        private ROUND_END_STATUS tempStatus = ROUND_END_STATUS.ON_GOING;
        private bool isEnded = false;

        //AutoNuker
        private bool isScientistAllDead = false;
        private bool isClassDAllDead = false;
        private bool isLocked = false;

        //EscapeCheck
        private bool isEscaper = false;
        private Vector escape_pos;
        private int escape_player_id;

        //DoorLocker
        private int doorlock_interval = 0;

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

        //CommandCoolTime
        private Dictionary<int, int> playeridlist = new Dictionary<int, int>();

        //Command
        private bool scp173_boosting = false;

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
                            plugin.Info($"InfoSender to Disabled(config:({ plugin.info_sender_to_ip})");
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

                        plugin.Debug($"[Infosender] {ip}:{port}");

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
                            plugin.Debug($"[Infosender] Called Abort");
                        }
                    }
                }
            }
        }

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            if (config_loaded)
            {
                plugin.Debug($"[InfoSender] Restarting...");
                infosender.Abort();
                infosender = null;

                infosender = new Thread(new ThreadStart(Sender));
                infosender.Start();
                plugin.Debug($"[InfoSender] Restart Completed");
            }

            SanyaPlugin.SetExtraDoorNames();
            roundduring = false;
            config_loaded = true;

            if (config_loaded)
            {
                plugin.Debug($"[ConfigLoader] Config Loaded");
            }

            genperm = new List<string>(plugin.ConfigManager.Config.GetListValue("generator_keycard_perm", new string[] { "ARMORY_LVL_3" }, false));
            friendlyfire = plugin.ConfigManager.Config.GetBoolValue("friendly_fire", false);
            startwait = plugin.ConfigManager.Config.GetIntValue("173_door_starting_cooldown", 25) * 3;

            int sum = 0;
            int eventc = -1;
            foreach (int cur in plugin.event_mode_weight)
            {
                eventc++;
                if (cur <= 0)
                {
                    plugin.Debug($"[RandomEventer] Skipped:{(SANYA_GAME_MODE)(eventc)}");
                    continue;
                }
                else
                {
                    plugin.Debug($"[RandomEventer] Added:{(SANYA_GAME_MODE)(eventc)}");
                    sum += cur;
                }
            }

            int random = rnd.Next(0, sum);
            for (int i = 0; i < plugin.event_mode_weight.Length; i++)
            {
                if (plugin.event_mode_weight[i] <= 0) continue;

                if (random < plugin.event_mode_weight[i])
                {
                    eventmode = (SANYA_GAME_MODE)i;
                    break;
                }
                random -= plugin.event_mode_weight[i];
            }

            if (eventmode == SANYA_GAME_MODE.NULL)
            {
                eventmode = SANYA_GAME_MODE.NORMAL;
            }

            if (eventmode == SANYA_GAME_MODE.STORY)
            {
                foreach (Camera079 item in Scp079PlayerScript.allCameras)
                {
                    if (item.cameraName.StartsWith("173 HALLWAY"))
                    {
                        cam173hall = new Vector(item.transform.position.x, item.transform.position.y - 2, item.transform.position.z);
                    }
                    else if (item.cameraName.StartsWith("173 CHAMBER"))
                    {
                        cam173chamber = new Vector(item.transform.position.x, item.transform.position.y - 1, item.transform.position.z);
                    }
                }
            }
            else if (eventmode == SANYA_GAME_MODE.CLASSD_INSURGENCY)
            {
                foreach (Room item in plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.SPEAKER))
                {
                    if (item.RoomType == RoomType.LCZ_ARMORY)
                    {
                        armory = new Vector(item.Position.x, item.Position.y + 2, item.Position.z);
                    }
                }
            }
            else if (eventmode == SANYA_GAME_MODE.HCZ_NOGUARD)
            {
                foreach (Room item in plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA))
                {
                    if (item.ZoneType == ZoneType.HCZ && (item.RoomType == RoomType.X_INTERSECTION || item.RoomType == RoomType.T_INTERSECTION || item.RoomType == RoomType.STRAIGHT))
                    {
                        hcz_hallways.Add(item);
                    }
                }
            }

            plugin.Info($"[RandomEventer] Selected:{eventmode.ToString()}");
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            updatecounter = 0;

            lcz_lights.AddRange(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA));
            lcz_lights = lcz_lights.FindAll(items => { return items.ZoneType == ZoneType.LCZ; });

            itemtable = GameObject.Find("Host").GetComponent<Inventory>().availableItems;

            tempStatus = ROUND_END_STATUS.ON_GOING;
            isEnded = false;
            isScientistAllDead = false;
            isClassDAllDead = false;
            isLocked = false;
            roundduring = true;

            plugin.Info($"RoundStart!");
            plugin.Info($"Class-D:{plugin.Round.Stats.ClassDAlive} Scientist:{plugin.Round.Stats.ScientistsAlive} NTF:{plugin.Round.Stats.NTFAlive} SCP:{plugin.Round.Stats.SCPAlive} CI:{plugin.Round.Stats.CiAlive}");

            if (eventmode != SANYA_GAME_MODE.STORY)
            {
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
                            ritem = (ItemType)plugin.classd_startitem_ok_itemid;
                        }
                        else
                        {
                            ritem = (ItemType)plugin.classd_startitem_no_itemid;
                        }

                        if (ritem >= 0)
                        {
                            plugin.Server.Map.SpawnItem((ItemType)ritem, spawnpos, new Vector(0, 0, 0));
                        }
                    }
                    plugin.Info($"Class-D-Contaiment Item Droped! (Success:{success_count})");
                }
            }


            if (eventmode == SANYA_GAME_MODE.CLASSD_INSURGENCY)
            {
                foreach (Smod2.API.Item i in plugin.Server.Map.GetItems(ItemType.MP4, true))
                {
                    Vector v = i.GetPosition();
                    for (int x = 0; x < plugin.classd_ins_items; x++)
                    {
                        plugin.Server.Map.SpawnItem(ItemType.MP4, new Vector(v.x, v.y + 1, v.z), new Vector(0, 0, 0));
                    }
                }

                foreach (Smod2.API.Item i in plugin.Server.Map.GetItems(ItemType.P90, true))
                {
                    Vector v = i.GetPosition();
                    for (int x = 0; x < plugin.classd_ins_items; x++)
                    {
                        plugin.Server.Map.SpawnItem(ItemType.CHAOS_INSURGENCY_DEVICE, new Vector(v.x, v.y + 1, v.z), new Vector(0, 0, 0));
                    }
                }
            }
            else if(eventmode == SANYA_GAME_MODE.STORY)
            {
                System.Timers.Timer t = new System.Timers.Timer
                {
                    Interval = 50,
                    AutoReset = false,
                    Enabled = true
                };
                t.Elapsed += delegate
                {
                    List<Smod2.API.Player> plys = plugin.Server.GetPlayers();
                    List<Smod2.API.Player> scps = plys.FindAll(x => x.TeamRole.Team == Smod2.API.Team.SCP);
                    int scpamount = scps.Count;
                    int scp173amount = scps.FindAll(x => x.TeamRole.Role == Role.SCP_173).Count;
                    int scp079amount = scps.FindAll(x => x.TeamRole.Role == Role.SCP_079).Count;

                    foreach (var ply in plys)
                    {
                        plugin.Debug($"[scps]{ply.Name} : {ply.TeamRole.Team} : {ply.TeamRole.Role}");
                    }
                    plugin.Debug($"[amount]{scpamount} : {scp173amount} : {scp079amount}");

                    for(int i = 0; i < scpamount; i++)
                    {
                        plugin.Debug($"target[{i}]:{scps[i].Name}[{scps[i].TeamRole.Role}]");

                        if(scp173amount == 0 || i == 0)
                        {
                            if (scps[i].TeamRole.Role == Role.SCP_173) continue;
                            if (scps[i].TeamRole.Role == Role.SCP_079) scp079amount--;
                            plugin.Debug($"change173[{i}]:{scps[i].Name}");
                            scps[i].ChangeRole(Role.SCP_173);
                            scp173amount++;
                            continue;
                        }

                        if(scp079amount == 0 && i > 0)
                        {
                            if (scps[i].TeamRole.Role == Role.SCP_079) continue;
                            if (scps[i].TeamRole.Role == Role.SCP_173) scp173amount--;
                            plugin.Debug($"change079[{i}]:{scps[i].Name}");
                            scps[i].ChangeRole(Role.SCP_079);
                            scp079amount++;
                            continue;
                        }

                        //Other SCP = 372CONTAIN
                        RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                        foreach (var pos in rnde.posIds)
                        {
                            if (pos.posID == "RandomPistol" && pos.position.position.y > 0.5f && pos.position.position.y < 0.7f)
                            {
                                scps[i].Teleport(new Vector(pos.position.position.x, pos.position.position.y + 1, pos.position.position.z));
                            }
                        }
                    }
                    t.Enabled = false;
                };
            }
        }

        public void OnCheckRoundEnd(CheckRoundEndEvent ev)
        {
            if (plugin.summary_less_mode)
            {
                if (tempStatus != ev.Status && !isEnded)
                {
                    if (tempStatus == ROUND_END_STATUS.ON_GOING)
                    {
                        plugin.Info($"Round Ended [NonSummary-Mode] [{ev.Status}]");
                        plugin.Info($"Class-D:{ev.Round.Stats.ClassDAlive} Scientist:{ev.Round.Stats.ScientistsAlive} NTF:{ev.Round.Stats.NTFAlive} SCP:{ev.Round.Stats.SCPAlive} CI:{ev.Round.Stats.CiAlive}");

                        UnityEngine.GameObject.Find("Host").GetComponent<MTFRespawn>().SetDecontCooldown(60f);
                        if (plugin.endround_all_godmode)
                        {
                            foreach (Player item in plugin.Server.GetPlayers())
                            {
                                item.SetGodmode(true);
                            }
                        }
                    }
                    tempStatus = ev.Status;
                }

                if (ev.Status != ROUND_END_STATUS.ON_GOING)
                {
                    ev.Status = ROUND_END_STATUS.ON_GOING;
                    isEnded = true;
                }
            }
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (roundduring)
            {
                plugin.Info($"Round Ended [{ev.Status}]");
                plugin.Info($"Class-D:{ev.Round.Stats.ClassDAlive} Scientist:{ev.Round.Stats.ScientistsAlive} NTF:{ev.Round.Stats.NTFAlive} SCP:{ev.Round.Stats.SCPAlive} CI:{ev.Round.Stats.CiAlive}");

                if (plugin.endround_all_godmode)
                {
                    foreach (Player item in plugin.Server.GetPlayers())
                    {
                        item.SetGodmode(true);
                    }
                }
            }
            roundduring = false;
        }

        public void OnRoundRestart(RoundRestartEvent ev)
        {
            plugin.Info($"RoundRestart...");
            roundduring = false;
            hcz_hallways.Clear();
            lcz_lights.Clear();
            playeridlist.Clear();
            gencomplete = false;
            lure_id = -1;
            chamber173count = 0;
            hall173count = 0;
            armory = null;
            cam173hall = null;
            cam173chamber = null;
            storyshaker = false;
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            plugin.Info($"[PlayerJoin] {ev.Player.Name}[{ev.Player.IpAddress}]({ev.Player.SteamId})");

            if (debug && ev.Player.GetRankName().Length == 0)
            {
                ev.Player.SetRank("nickel", "TESTER", "tester");
                plugin.Debug($"TESTER GRANTED:{ev.Player.ToString()}");
            }

            if (plugin.spectator_slot > 0)
            {
                if (playingList.Count >= plugin.Server.MaxPlayers - plugin.spectator_slot)
                {
                    plugin.Info($"[Spectator] Join to Spectator : {ev.Player.Name}({ev.Player.SteamId})");
                    ev.Player.OverwatchMode = true;
                    ev.Player.SetRank("nickel", "SPECTATOR", "");
                    spectatorList.Add(ev.Player);
                }
                else
                {
                    plugin.Info($"[Spectator] Join to Player : {ev.Player.Name}({ev.Player.SteamId})");
                    ev.Player.OverwatchMode = false;
                    playingList.Add(ev.Player);
                }
            }
        }

        public void OnDecontaminate()
        {
            plugin.Info($"LCZ Decontaminated");

            if (plugin.cassie_subtitle && roundduring)
            {
                plugin.Server.Map.ClearBroadcasts();
                plugin.Server.Map.Broadcast(13, $"<size=25>《下層がロックされ、「再収容プロトコル」の準備が出来ました。全ての有機物は破壊されます。》\n </size><size=20>《Light Containment Zone is locked down and ready for decontamination. The removal of organic substances has now begun.》\n</size>", false);
            }
        }

        public void OnDetonate()
        {
            plugin.Info($"AlphaWarhead Denotated");
        }

        public void OnStartCountdown(WarheadStartEvent ev)
        {
            plugin.Debug($"[OnStartCountdown]");

            if (!ev.Cancel)
            {
                if (plugin.cassie_subtitle && roundduring)
                {
                    plugin.Server.Map.ClearBroadcasts();
                    if (!ev.IsResumed)
                    {
                        if (ev.Activator != null)
                        {
                            plugin.Server.Map.Broadcast(15, $"<color=#ff0000><size=25>《[{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]により「AlphaWarhead」の緊急起爆シーケンスが開始されました。\n施設の地下区画は、約90秒後に爆破されます。》\n</size><size=20>《Alpha Warhead emergency detonation sequence engaged by [{ev.Activator.Name}/{ev.Activator.TeamRole.Team}].\nThe underground section of the facility will be detonated in t-minus 90 seconds.》\n</size></color>", false);
                        }
                        else
                        {
                            if (plugin.original_auto_nuke)
                            {
                                if (isLocked)
                                {
                                    plugin.Server.Map.Broadcast(15, $"<color=#ff0000><size=25>【セクター2/停止不可】\n《施設システムにより「AlphaWarhead」の緊急起爆シーケンスが開始されました。\n施設の地下区画は、約90秒後に爆破されます。》\n</size><size=20>【Sector 2】\n《Alpha Warhead emergency detonation sequence engaged by Facility-Systems.\nThe underground section of the facility will be detonated in t-minus 90 seconds.》\n</size></color>", false);
                                }
                                else
                                {
                                    plugin.Server.Map.Broadcast(15, $"<color=#ff0000><size=25>【セクター1/停止可能】\n《施設システムにより「AlphaWarhead」の緊急起爆シーケンスが開始されました。\n施設の地下区画は、約90秒後に爆破されます。》\n</size><size=20>【Sector 1】\n《Alpha Warhead emergency detonation sequence engaged by Facility-Systems.\nThe underground section of the facility will be detonated in t-minus 90 seconds.》\n</size></color>", false);
                                }
                            }
                            else
                            {
                                plugin.Server.Map.Broadcast(15, $"<color=#ff0000><size=25>《施設システムにより「AlphaWarhead」の緊急起爆シーケンスが開始されました。\n施設の地下区画は、約90秒後に爆破されます。》\n</size><size=20>《Alpha Warhead emergency detonation sequence engaged by Facility-Systems.\nThe underground section of the facility will be detonated in t-minus 90 seconds.》\n</size></color>", false);
                            }
                        }
                    }
                    else
                    {
                        double left = ev.TimeLeft;
                        double count = Math.Truncate(left / 10.0) * 10.0;
                        plugin.Server.Map.ClearBroadcasts();
                        if (ev.Activator != null)
                        {
                            plugin.Server.Map.Broadcast(10, $"<color=#ff0000><size=25>《[{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]により緊急起爆シーケンスが再開されました。約{count.ToString()}秒後に爆破されます。》\n</size><size=20>《Detonation sequence resumed by [{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]. t-minus {count.ToString()} seconds.》\n</size></color>", false);
                        }
                        else
                        {
                            if (plugin.original_auto_nuke)
                            {
                                if (isLocked)
                                {
                                    plugin.Server.Map.Broadcast(10, $"<color=#ff0000><size=25>【セクター2/停止不可】\n《施設システムにより緊急起爆シーケンスが再開されました。約{count.ToString()}秒後に爆破されます。》\n</size><size=20>【Sector 2】\n《Detonation sequence resumed by Facility-Systems. t-minus {count.ToString()} seconds.》\n</size></color>", false);
                                }
                                else
                                {
                                    plugin.Server.Map.Broadcast(10, $"<color=#ff0000><size=25>【セクター1/停止可能】\n《施設システムにより緊急起爆シーケンスが再開されました。約{count.ToString()}秒後に爆破されます。》\n</size><size=20>【Sector 1】\n《Detonation sequence resumed by Facility-Systems. t-minus {count.ToString()} seconds.》\n</size></color>", false);
                                }
                            }
                            else
                            {
                                plugin.Server.Map.Broadcast(10, $"<color=#ff0000><size=25>《施設システムにより緊急起爆シーケンスが再開されました。約{count.ToString()}秒後に爆破されます。》\n</size><size=20>《Detonation sequence resumed by Facility-Systems. t-minus {count.ToString()} seconds.》\n</size></color>", false);
                            }
                        }
                    }
                }
            }
        }

        public void OnStopCountdown(WarheadStopEvent ev)
        {
            plugin.Debug($"[OnStopCountdown]");

            if (!ev.Cancel)
            {
                if (plugin.cassie_subtitle && roundduring)
                {
                    plugin.Server.Map.ClearBroadcasts();
                    if (ev.Activator != null)
                    {
                        plugin.Server.Map.Broadcast(7, $"<color=#ff0000><size=25>《[{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]により起爆が取り消されました。システムを再起動します。》\n</size><size=20>《Detonation cancelled by [{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]. Restarting systems.》\n</size></color>", false);
                    }
                    else
                    {
                        plugin.Server.Map.Broadcast(7, $"<color=#ff0000><size=25>《施設システムにより起爆が取り消されました。システムを再起動します。》\n</size><size=20>《Detonation cancelled by Facility-Systems. Restarting systems.》\n</size></color>", false);
                    }
                }
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
                        plugin.Server.Map.Broadcast(30, $"<color=#6c80ff><size=25>《機動部隊イプシロン-11「{ev.Unit}」が施設に到着しました。\n残りの全職員は、機動部隊が貴方の場所へ到着するまで「標準避難プロトコル」の続行を推奨します。\n「{plugin.Server.Round.Stats.SCPAlive}」オブジェクトが再収容されていません。》\n</size><size=20>《Mobile Task Force Unit, Epsilon-11, designated, '{ev.Unit}', has entered the facility.\nAll remaining personnel are advised to proceed with standard evacuation protocols until an MTF squad reaches your destination.\nAwaiting recontainment of: {plugin.Server.Round.Stats.SCPAlive} SCP subject.》\n</size></color>", false);
                    }
                    else
                    {
                        plugin.Server.Map.Broadcast(30, $"<color=#6c80ff><size=25>《機動部隊イプシロン-11「{ev.Unit}」が施設に到着しました。\n残りの全職員は、機動部隊が貴方の場所へ到着するまで「標準避難プロトコル」の続行を推奨します。\n重大な脅威が施設内に存在します。注意してください。》\n </size><size=20>《Mobile Task Force Unit, Epsilon-11, designated, '{ev.Unit}', has entered the facility.\nAll remaining personnel are advised to proceed with standard evacuation protocols, until MTF squad has reached your destination.\nSubstantial threat to safety is within the facility -- Exercise caution.》\n</size></color>", false);
                    }
                }
            }
        }

        public void OnCheckEscape(PlayerCheckEscapeEvent ev)
        {
            plugin.Debug($"[OnCheckEscape] {ev.Player.Name}:{ev.Player.TeamRole.Role}");

            if (!roundduring)
            {
                ev.AllowEscape = false;
                return;
            }

            isEscaper = true;
            escape_player_id = ev.Player.PlayerId;
            escape_pos = ev.Player.GetPosition();

            plugin.Debug($"[OnCheckEscape] escaper x:{escape_pos.x} y:{escape_pos.y} z:{escape_pos.z}");
        }

        public void OnSetRole(PlayerSetRoleEvent ev)
        {
            plugin.Debug($"[OnSetRole] {ev.Player.Name}:{ev.Role}");

            //------------------------------------EventTeleporter---------------------------
            if (eventmode == SANYA_GAME_MODE.STORY)
            {
                if (cam173chamber != null && ev.Role == Role.CLASSD && chamber173count < 4)
                {
                    ev.Player.Teleport(cam173chamber);
                    chamber173count++;
                }

                if (cam173hall != null && ev.Role == Role.SCIENTIST && hall173count < 4)
                {
                    ev.Player.Teleport(cam173hall);
                    hall173count++;
                }
            }
            else if (eventmode == SANYA_GAME_MODE.CLASSD_INSURGENCY)
            {
                if (armory != null && ev.Role == Role.CLASSD)
                {
                    ev.Player.Teleport(armory);
                }
            }
            else if (eventmode == SANYA_GAME_MODE.HCZ_NOGUARD)
            {
                if (hcz_hallways != null && (ev.Role == Role.CLASSD || ev.Role == Role.SCIENTIST))
                {
                    ev.Player.Teleport(hcz_hallways[rnd.Next(0, hcz_hallways.Count-1)].Position,true);
                }
                else if (hcz_hallways != null && ev.Role == Role.FACILITY_GUARD)
                {
                    ev.Role = Role.SCIENTIST;

                    System.Timers.Timer t = new System.Timers.Timer
                    {
                        Interval = 50,
                        AutoReset = false,
                        Enabled = true
                    };
                    t.Elapsed += delegate
                    {
                        ev.Player.Teleport(hcz_hallways[rnd.Next(0, hcz_hallways.Count - 1)].Position, true);
                        t.Enabled = false;
                    };
                }
            }

            //---------EscapeChecker---------
            if (isEscaper)
            {
                if (escape_player_id == ev.Player.PlayerId)
                {
                    if (plugin.escape_spawn && ev.Role != Role.CHAOS_INSURGENCY)
                    {
                        plugin.Debug($"[OnSetRole] escaper_id:{escape_player_id} / spawn_id:{ev.Player.PlayerId}");
                        plugin.Info($"[EscapeChecker] Escape Successfully [{ev.Player.Name}:{ev.Role}]");
                        ev.Player.Teleport(escape_pos, false);
                        isEscaper = false;
                    }
                    else
                    {
                        plugin.Info($"[EscapeChecker] Disabled (config or CHAOS) [{ev.Player.Name}:{ev.Role}]");
                        isEscaper = false;
                    }
                }
            }

            //---------------------DefaultAmmo---------------------
            int[] targetammo = new int[] { 0, 0, 0 };
            if (ev.Role == Role.CLASSD)
            {
                targetammo = plugin.default_ammo_classd;
                if (eventmode == SANYA_GAME_MODE.CLASSD_INSURGENCY)
                {
                    targetammo[(int)AmmoType.DROPPED_7] += 35 * 4;
                }
            }
            else if (ev.Role == Role.SCIENTIST)
            {
                targetammo = plugin.default_ammo_scientist;
            }
            else if (ev.Role == Role.FACILITY_GUARD)
            {
                targetammo = plugin.default_ammo_guard;
            }
            else if (ev.Role == Role.CHAOS_INSURGENCY)
            {
                targetammo = plugin.default_ammo_ci;
            }
            else if (ev.Role == Role.NTF_CADET)
            {
                targetammo = plugin.default_ammo_cadet;
            }
            else if (ev.Role == Role.NTF_LIEUTENANT)
            {
                targetammo = plugin.default_ammo_lieutenant;
            }
            else if (ev.Role == Role.NTF_COMMANDER)
            {
                targetammo = plugin.default_ammo_commander;
            }
            else if (ev.Role == Role.NTF_SCIENTIST)
            {
                targetammo = plugin.default_ammo_ntfscientist;
            }
            ev.Player.SetAmmo(AmmoType.DROPPED_5, targetammo[(int)AmmoType.DROPPED_5]);
            ev.Player.SetAmmo(AmmoType.DROPPED_7, targetammo[(int)AmmoType.DROPPED_7]);
            ev.Player.SetAmmo(AmmoType.DROPPED_9, targetammo[(int)AmmoType.DROPPED_9]);
            plugin.Debug($"[SetAmmo] {ev.Player.Name}({ev.Role}) 5.56mm:{targetammo[(int)AmmoType.DROPPED_5]} 7.62mm:{targetammo[(int)AmmoType.DROPPED_7]} 9mm:{targetammo[(int)AmmoType.DROPPED_9]}");

        }

        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            plugin.Debug($"[OnPlayerHurt] Before {ev.Attacker.Name}<{ev.Attacker.TeamRole.Role}>({ev.DamageType}:{ev.Damage}) -> {ev.Player.Name}<{ev.Player.TeamRole.Role}>");

            //----------------------------LureSpeak-----------------------
            if (plugin.scp106_lure_speaktime > 0 && roundduring)
            {
                if (ev.DamageType == DamageType.LURE && lure_id == -1)
                {
                    if (plugin.Server.Map.GetIntercomSpeaker() == null)
                    {
                        plugin.Info($"[106Lure] Contained({ev.Player.Name}):Start Speaking...({plugin.scp106_lure_speaktime}seconds)");
                        ev.Damage = 0.0f;
                        lure_id = ev.Player.PlayerId;
                        ev.Player.SetGodmode(true);
                        foreach (Smod2.API.Item hasitem in ev.Player.GetInventory())
                        {
                            hasitem.Remove();
                        }
                        ev.Player.PersonalClearBroadcasts();
                        ev.Player.PersonalBroadcast((uint)plugin.scp106_lure_speaktime, $"<color=#ffff00><size=25>《あなたはSCP-106の再収容に使用されます。最後に{plugin.scp106_lure_speaktime}秒の放送時間が与えられます。》\n</size><size=20>《You will be used for recontainment SCP-106. You can broadcast for {plugin.scp106_lure_speaktime} seconds.》\n</size></color>", false);

                        plugin.Server.Map.SetIntercomSpeaker(ev.Player);

                        System.Timers.Timer t = new System.Timers.Timer
                        {
                            Interval = plugin.scp106_lure_speaktime * 1000,
                            AutoReset = false,
                            Enabled = true
                        };
                        t.Elapsed += delegate
                        {
                            plugin.Info($"[106Lure] Contained({ev.Player.Name}):Speaking ended");
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
                    ev.DamageType != DamageType.NONE &&
                    ev.Attacker.Name != "Server" &&
                    ev.Attacker.TeamRole.Team == ev.Player.TeamRole.Team &&
                    ev.Attacker.PlayerId != ev.Player.PlayerId &&
                    ev.Player.GetGodmode() == false &&
                    roundduring)
                {
                    plugin.Info($"[FriendlyFire] {ev.Attacker.Name}({ev.DamageType}:{ev.Damage}) -> {ev.Player.Name}");
                    ev.Attacker.PersonalClearBroadcasts();
                    ev.Attacker.PersonalBroadcast(5, $"<color=#ff0000><size=25>《誤射に注意。味方へのダメージは容認されません。(<{ev.Player.Name}>への攻撃。)》\n</size><size=20>《Check your fire! Damage to ally forces not be tolerated.(Damaged to <{ev.Player.Name}>)》\n</size></color>", false);
                }
            }

            if (ev.Player.TeamRole.Role == Role.SCP_096)
            {
                if (Scp096PlayerScript.instance != null)
                {
                    Scp096PlayerScript.instance.IncreaseRage(1f);
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

            //ロール除算計算開始
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
            plugin.Debug($"[OnPlayerHurt] After {ev.Attacker.Name}<{ev.Attacker.TeamRole.Role}>({ev.DamageType}:{ev.Damage}) -> {ev.Player.Name}<{ev.Player.TeamRole.Role}>");
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            plugin.Debug($"[OnPlayerDie] {ev.Killer}:{ev.DamageTypeVar} -> {ev.Player.Name}");

            if (plugin.Server.Map.GetIntercomSpeaker() != null)
            {
                if (ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                {
                    plugin.Server.Map.SetIntercomSpeaker(null);
                }
            }

            //----------------------------------------------------キル回復------------------------------------------------  
            //############## SCP-173 ###############
            if (!scp173_boosting &&                          //boostコマンド
                ev.DamageTypeVar == DamageType.SCP_173 &&
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
                plugin.Info($"[Infector] 049-2 Infected!(Limit:{plugin.infect_limit_time}s) [{ev.Killer.Name}->{ev.Player.Name}]");
                ev.DamageTypeVar = DamageType.SCP_049;
                ev.Player.Infect(plugin.infect_limit_time);
            }
            //----------------------------------------------------ペスト治療------------------------------------------------

            //----------------------------------------------------PocketCleaner---------------------------------------------
            if (ev.DamageTypeVar == DamageType.POCKET && plugin.scp106_cleanup)
            {
                plugin.Info($"[PocketCleaner] Cleaned ({ev.Player.Name})");
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
            plugin.Info($"[Infector] 049 Infected!(Limit:{plugin.infect_limit_time}s) [{ev.Attacker.Name}->{ev.Player.Name}]");
            ev.InfectTime = plugin.infect_limit_time;
        }

        public void OnPocketDimensionDie(PlayerPocketDimensionDieEvent ev)
        {
            plugin.Debug($"[OnPocketDie] {ev.Player.Name}");

            //------------------------------ディメンション死亡回復(SCP-106)-----------------------------------------
            if (plugin.recovery_amount_scp106 > 0)
            {
                try
                {
                    foreach (Player ply in plugin.Server.GetPlayers(Role.SCP_106))
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
                catch (Exception)
                {

                }
            }
        }

        public void OnRecallZombie(PlayerRecallZombieEvent ev)
        {
            plugin.Debug($"[OnRecallZombie] {ev.Player.Name} -> {ev.Target.Name} ({ev.AllowRecall})");

            if (ev.AllowRecall)
            {
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

                (ev.Target.GetGameObject() as GameObject).GetComponent<CharacterClassManager>().NetworkdeathPosition = new Vector3(ev.Player.GetPosition().x, ev.Player.GetPosition().y, ev.Player.GetPosition().z);
            }
            else
            {
                ev.Player.PersonalClearBroadcasts();
                if (plugin.Server.GetPlayers().FindIndex(x => x.Name == ev.Target.Name) == -1)
                {
                    ev.Player.PersonalBroadcast(5, "<size=25>《治療失敗。対象プレイヤーは切断済みです。》\n </size><size=20>《Recall failed.》\n</size>", false);
                }
                else
                {
                    ev.Player.PersonalBroadcast(5, "<size=25>《治療失敗。対象プレイヤーはリスポーン済みです。》\n </size><size=20>《Recall failed.》\n</size>", false);
                }
            }
        }

        public void OnDoorAccess(PlayerDoorAccessEvent ev)
        {
            plugin.Debug($"[OnDoorAccess] {ev.Player.Name}:{ev.Door.Name}({ev.Door.Open}):{ev.Door.Permission}={ev.Allow}");

            //plugin.Debug($"name:{(ev.Door.GetComponent() as Door).name} parent:{(ev.Door.GetComponent() as Door).transform.parent.name} par-parent:{(ev.Door.GetComponent() as Door).transform.parent.parent.name} type:{(ev.Door.GetComponent() as Door).doorType}");

            if (plugin.inventory_card_act)
            {
                if (ev.Player.TeamRole.Team != Smod2.API.Team.SCP && !ev.Player.GetBypassMode() && !ev.Door.Locked)
                {
                    List<string> permlist = new List<string>();
                    foreach (Smod2.API.Item i in ev.Player.GetInventory())
                    {
                        foreach (Item item in itemtable)
                        {
                            if (item.id == (int)i.ItemType)
                            {
                                foreach (string p in item.permissions)
                                {
                                    if (permlist.IndexOf(p) == -1)
                                    {
                                        permlist.Add(p);
                                    }
                                }
                            }
                        }
                    }

                    plugin.Debug($"SanyaChecker:{SanyaPlugin.CanOpenDoor(permlist.ToArray(), ev.Door).ToString()}");
                    ev.Allow = SanyaPlugin.CanOpenDoor(permlist.ToArray(), ev.Door);
                }
            }

            if (eventmode == SANYA_GAME_MODE.HCZ_NOGUARD)
            {
                if (ev.Player.TeamRole.Team != Smod2.API.Team.SCP)
                {
                    foreach (Smod2.API.Item i in ev.Player.GetInventory())
                    {
                        if (i.ItemType == ItemType.SCIENTIST_KEYCARD && ev.Door.Name.StartsWith("CHECKPOINT_LCZ"))
                        {
                            plugin.Debug("[Eventer]HCZ-ScientistCard-Allow");
                            ev.Allow = true;
                        }
                    }
                }
            }

            if(eventmode == SANYA_GAME_MODE.STORY || eventmode == SANYA_GAME_MODE.HCZ_NOGUARD)
            {
                if (ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                {
                    if (startwait > plugin.Round.Duration)
                    {
                        plugin.Debug($"[Eventer]Too fast SCP : {startwait} > {plugin.Round.Duration}");
                        ev.Allow = false;
                    }
                }
            }

            if ((int)plugin.doorlock_itemid > -1)
            {
                if (doorlock_interval >= plugin.doorlock_interval_second)
                {
                    if (ev.Player.TeamRole.Role == Role.NTF_COMMANDER)
                    {
                        if (ev.Player.GetCurrentItemIndex() > -1)
                        {
                            if (ev.Player.GetCurrentItem().ItemType == (ItemType)plugin.doorlock_itemid)
                            {
                                if (!ev.Door.Name.Contains("CHECKPOINT"))
                                {
                                    plugin.Info($"[DoorLock] {ev.Player.Name} lock {ev.Door.Name}");
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
            plugin.Debug($"[OnPlayerRadioSwitch] " + ev.Player.Name + ":" + ev.ChangeTo);

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
            plugin.Debug($"[OnElevatorUse] {ev.Player.Name} : {ev.Elevator.ElevatorType}[{ev.Elevator.ElevatorStatus}] - {ev.Elevator.MovingSpeed} : {ev.AllowUse}");

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
            plugin.Debug($"[On106CreatePortal] {ev.Player.Name} [{ev.Player.Get106Portal().x}/{ev.Player.Get106Portal().y}/{ev.Player.Get106Portal().z}] - ({ev.Position.x}/{ev.Position.y}/{ev.Position.z})");

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

                            if (roundduring)
                            {
                                if (plugin.Server.Round.Duration > plugin.scp106_portal_to_human_wait)
                                {
                                    if (humanlist.Count <= 0)
                                    {
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(5, $"<size=25>《ターゲットが見つからないようだ。》\n </size><size=20>《Target not found.》\n</size>", false);
                                        plugin.Info("[106Portal] No Humans");
                                    }
                                    else
                                    {
                                        int rndresult = rnd.Next(0, humanlist.Count);
                                        Vector temppos = new Vector(humanlist[rndresult].GetPosition().x, humanlist[rndresult].GetPosition().y - 2.3f, humanlist[rndresult].GetPosition().z);
                                        ev.Position = temppos;
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(5, $"<size=25>《生存者<{humanlist[rndresult].Name}>の近くにポータルを作成します。》\n </size><size=20>《Create portal at close to <{humanlist[rndresult].Name}>.》\n</size>", false);
                                        plugin.Info("[106Portal] Target:" + humanlist[rndresult].Name);
                                    }
                                }
                                else
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(5, $"<size=25>《まだ使えないようだ。》\n </size><size=20>《Not available yet.》\n</size>", false);
                                }
                            }
                        }
                        portaltemp = ev.Position;
                        portal_cooltime = 0;
                    }
                }
            }
        }

        public void OnSCP914Activate(SCP914ActivateEvent ev)
        {
            bool isAfterChange = false;
            Vector newpos = new Vector(ev.OutputPos.x, ev.OutputPos.y + 1.0f, ev.OutputPos.z);

            plugin.Debug($"[OnSCP914Activate] {ev.User.Name}/{ev.KnobSetting}");

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
                                plugin.Info($"[SCP914] COARSE:{player.Name}");

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
                                plugin.Info($"[SCP914] 1to1:{player.Name}");

                                List<Player> speclist = new List<Player>();
                                foreach (Player spec in plugin.Server.GetPlayers(Role.SPECTATOR))
                                {
                                    if (spec.OverwatchMode == false)
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

                                    foreach (Smod2.API.Item specitem in speclist[targetspec].GetInventory())
                                    {
                                        specitem.Remove();
                                    }
                                    foreach (Smod2.API.Item hasitem in player.GetInventory())
                                    {
                                        speclist[targetspec].GiveItem(hasitem.ItemType);
                                    }
                                    speclist[targetspec].SetHealth(player.GetHealth());
                                    speclist[targetspec].SetAmmo(AmmoType.DROPPED_5, player.GetAmmo(AmmoType.DROPPED_5));
                                    speclist[targetspec].SetAmmo(AmmoType.DROPPED_7, player.GetAmmo(AmmoType.DROPPED_7));
                                    speclist[targetspec].SetAmmo(AmmoType.DROPPED_9, player.GetAmmo(AmmoType.DROPPED_9));

                                    player.ChangeRole(Role.SPECTATOR, true, false, false, false);
                                    plugin.Info($"[SCP914] 1to1_Changed_to:{speclist[targetspec].Name}");
                                }
                                isAfterChange = true;
                            }
                            else if (ev.KnobSetting == KnobSetting.FINE)
                            {
                                plugin.Info($"[SCP914] FINE:{player.Name}");

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
                                plugin.Info($"[SCP914] VERY_FINE:{player.Name}");

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

        public void OnSummonVehicle(SummonVehicleEvent ev)
        {
            plugin.Debug($"[OnSummonVehicle] AllowSummon:{ev.AllowSummon}(isCi:{ev.IsCI}) : Config:{plugin.stop_mtf_after_nuke}(isDetonated:{plugin.Server.Map.WarheadDetonated})");

            if ((plugin.stop_mtf_after_nuke && plugin.Server.Map.WarheadDetonated) || !roundduring)
            {
                ev.AllowSummon = false;
            }
        }

        public void OnGeneratorAccess(PlayerGeneratorAccessEvent ev)
        {
            plugin.Debug($"[OnGeneratorAccess] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}:{ev.Generator.Engaged}");

            if (plugin.generator_engaged_cantopen)
            {
                if (ev.Generator.Engaged)
                {
                    ev.Allow = false;
                }
            }
        }

        public void OnGeneratorUnlock(PlayerGeneratorUnlockEvent ev)
        {
            plugin.Debug($"[OnGeneratorUnlock] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}:{ev.Generator.Engaged}");
            foreach (string p in genperm)
            {
                plugin.Debug($"genperm:{p}");
            }

            if (plugin.inventory_card_act)
            {
                if (ev.Player.TeamRole.Team != Smod2.API.Team.SCP && !ev.Generator.Engaged && !ev.Player.GetBypassMode())
                {
                    List<string> permlist = new List<string>();
                    foreach (Smod2.API.Item i in ev.Player.GetInventory())
                    {
                        foreach (Item item in itemtable)
                        {
                            if (item.id == (int)i.ItemType)
                            {
                                foreach (string p in item.permissions)
                                {
                                    if (permlist.IndexOf(p) == -1)
                                    {
                                        permlist.Add(p);
                                    }
                                }
                            }
                        }
                    }

                    plugin.Debug($"SanyaChecker:{SanyaPlugin.CanOpenDoor(permlist.ToArray(), genperm.ToArray()).ToString()}");
                    ev.Allow = SanyaPlugin.CanOpenDoor(permlist.ToArray(), genperm.ToArray());
                }
            }

            if (ev.Allow)
            {
                ev.Generator.Open = true;
            }
        }

        public void OnGeneratorInsertTablet(PlayerGeneratorInsertTabletEvent ev)
        {
            plugin.Debug($"[OnGeneratorInsertTablet] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}({ev.RemoveTablet})");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}:{ev.Generator.Engaged}");

            if (plugin.cassie_subtitle)
            {
                if (ev.Allow)
                {
                    string[] genname = SanyaPlugin.TranslateGeneratorName(ev.Generator.Room.RoomType);

                    if (roundduring)
                    {
                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast(10, $"<color=#bbee00><size=25>《<{genname[0]}>の発電機が起動を始めました。》\n</size><size=20>《Generator<{genname[1]}> has starting.》\n</size></color>", false);
                    }
                }
            }
        }

        public void OnGeneratorFinish(GeneratorFinishEvent ev)
        {
            plugin.Debug($"[OnGeneratorFinish] {ev.Generator.Room.RoomType}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}:{ev.Generator.Engaged}");

            int engcount = 1;
            foreach (Generator gens in plugin.Server.Map.GetGenerators())
            {
                if (gens.Engaged && ev.Generator.Room.RoomType != gens.Room.RoomType)
                {
                    engcount++;
                }
            }

            if (engcount >= 5)
            {
                gencomplete = true;
            }

            if (plugin.generator_engaged_cantopen)
            {
                ev.Generator.Open = false;
            }

            if (plugin.cassie_subtitle)
            {
                string[] genname = SanyaPlugin.TranslateGeneratorName(ev.Generator.Room.RoomType);

                if (roundduring)
                {
                    if (!gencomplete)
                    {
                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast(10, $"<color=#bbee00><size=25>《5つ中{engcount}つ目の発電機<{genname[0]}>の起動が完了しました。》\n</size><size=20>《{engcount} out of 5 generators activated. <{genname[1]}>》\n</size></color>", false);
                    }
                    else
                    {
                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast(20, $"<color=#bbee00><size=25>《5つ中{engcount}つ目の発電機<{genname[0]}>の起動が完了しました。\n全ての発電機が起動されました。最後再収容手順を開始します。\n中層は約一分後に過充電されます。》\n</size><size=20>《{engcount} out of 5 generators activated. <{genname[1]}>\nAll generators has been sucessfully engaged.\nFinalizing recontainment sequence.\nHeavy containment zone will overcharge in t-minus 1 minutes.》\n </size></color>", false);
                    }
                }
            }
        }

        public void On079Door(Player079DoorEvent ev)
        {
            plugin.Debug($"[On079Door] {ev.Player.Name} (Tier:{ev.Player.Scp079Data.Level + 1}:{ev.Door.Name}):{ev.Allow}");

            if (ev.Allow)
            {
                if (ev.Door.Locked)
                {
                    if (ev.Door.Open)
                    {
                        if (!(ev.Door.GetComponent() as Door).moving.moving)
                        {
                            ev.Door.Open = false;
                        }
                    }
                    else
                    {
                        if (!(ev.Door.GetComponent() as Door).moving.moving)
                        {
                            ev.Door.Open = true;
                        }
                    }
                }
            }
        }

        public void On079Lockdown(Player079LockdownEvent ev)
        {
            plugin.Debug($"[On079Lockdown] {ev.Player.Name}(Tier{ev.Player.Scp079Data.Level + 1}:{ev.Room.RoomType}({ev.APDrain}):{ev.Allow}");

            SanyaPlugin.CallAmbientSound((int)SANYA_AMBIENT_ID.BEEP_5);

            if (ev.Player.Scp079Data.Level >= 2)
            {
                plugin.Debug("079LockDown-HCZBlackout");
                Generator079.mainGenerator.CallRpcOvercharge();
                if (!plugin.Server.Map.LCZDecontaminated)
                {
                    plugin.Debug("079LockDown-LCZBlackout");
                    lcz_lights.ForEach(items => { items.FlickerLights(); });
                }
            }
        }

        public void On079StartSpeaker(Player079StartSpeakerEvent ev)
        {
            ev.APDrain = 0.0f;
            ev.Player.Scp079Data.SpeakerAPPerSecond = 0.0f;
            plugin.Debug($"[On079StartSpeaker] {ev.Player.Name}:{ev.Room.RoomType}({ev.APDrain}):{ev.Allow}");
        }

        public void OnUpdate(UpdateEvent ev)
        {
            List<int> plist = new List<int>(playeridlist.Keys.ToList());

            foreach (int item in plist)
            {
                if (playeridlist[item] > 0)
                {
                    playeridlist[item] -= 1;
                }
            }

            if (updatecounter % 60 == 0 && config_loaded)
            {
                updatecounter = 0;

                if(eventmode == SANYA_GAME_MODE.STORY || eventmode == SANYA_GAME_MODE.HCZ_NOGUARD)
                {
                    if(startwait < plugin.Round.Duration && !storyshaker)
                    {
                        plugin.Server.Map.Shake();
                        storyshaker = true;
                    }
                }

                if (plugin.summary_less_mode)
                {
                    if (roundduring && isEnded)
                    {
                        roundduring = false;

                        int restarttime = ConfigManager.Manager.Config.GetIntValue("auto_round_restart_time", 10);

                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast((uint)restarttime, $"<size=35>《ラウンドが終了しました。{restarttime}秒後にリスタートします。》\n </size><size=25>《Round Ended. Restart after {restarttime} seconds.》\n</size>", false);
                        EventManager.Manager.HandleEvent<IEventHandlerRoundEnd>(new RoundEndEvent(plugin.Server, plugin.Round, ROUND_END_STATUS.FORCE_END));

                        System.Timers.Timer t = new System.Timers.Timer
                        {
                            Interval = restarttime * 1000,
                            AutoReset = false,
                            Enabled = true
                        };
                        t.Elapsed += delegate
                        {
                            RoundSummary.singleton.CallRpcDimScreen();
                            ServerConsole.AddLog("Round restarting");
                            plugin.Round.RestartRound();
                            t.Enabled = false;
                        };
                    }
                }

                if (plugin.spectator_slot > 0)
                {
                    plugin.Debug($"[SpectatorCheck] playing:{playingList.Count} spectator:{spectatorList.Count}");

                    List<Player> players = plugin.Server.GetPlayers();

                    foreach (Player ply in playingList.ToArray())
                    {
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug($"[SpectatorCheck] delete player:{ply.SteamId}");
                            playingList.Remove(ply);
                        }
                    }

                    foreach (Player ply in spectatorList.ToArray())
                    {
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug($"[SpectatorCheck] delete spectator:{ply.SteamId}");
                            spectatorList.Remove(ply);
                        }
                    }

                    if (playingList.Count < plugin.Server.MaxPlayers - plugin.spectator_slot)
                    {
                        if (spectatorList.Count > 0)
                        {
                            plugin.Info($"[SpectatorCheck] Spectator to Player:{spectatorList[0].Name}({spectatorList[0].SteamId})");
                            spectatorList[0].OverwatchMode = false;
                            spectatorList[0].SetRank();
                            spectatorList[0].HideTag(true);
                            playingList.Add(spectatorList[0]);
                            spectatorList.RemoveAt(0);
                        }
                    }

                }

                if (plugin.original_auto_nuke)
                {
                    if (roundduring)
                    {
                        if (!AlphaWarheadController.host.inProgress)
                        {
                            if (plugin.Round.Stats.ScientistsAlive < 1 && !isScientistAllDead && plugin.Server.Map.LCZDecontaminated)
                            {
                                plugin.Info("[AutoNuke] Sector 1");
                                plugin.Info($"Class-D:{plugin.Round.Stats.ClassDAlive} Scientist:{plugin.Round.Stats.ScientistsAlive} NTF:{plugin.Round.Stats.NTFAlive} SCP:{plugin.Round.Stats.SCPAlive} CI:{plugin.Round.Stats.CiAlive}");

                                isScientistAllDead = true;
                                AlphaWarheadController.host.InstantPrepare();
                                AlphaWarheadController.host.StartDetonation();
                            }
                            else
                            {
                                if (plugin.Round.Stats.ClassDAlive < 1 && plugin.Round.Stats.NTFAlive < 5 && !isClassDAllDead && isScientistAllDead)
                                {
                                    plugin.Info("[AutoNuke] Sector 2");
                                    plugin.Info($"Class-D:{plugin.Round.Stats.ClassDAlive} Scientist:{plugin.Round.Stats.ScientistsAlive} NTF:{plugin.Round.Stats.NTFAlive} SCP:{plugin.Round.Stats.SCPAlive} CI:{plugin.Round.Stats.CiAlive}");

                                    isLocked = true;
                                    isClassDAllDead = true;
                                    AlphaWarheadController.host.InstantPrepare();
                                    AlphaWarheadController.host.StartDetonation();
                                    AlphaWarheadController.host.SetLocked(true);
                                }
                            }
                        }
                    }
                }

                if (plugin.night_mode || eventmode == SANYA_GAME_MODE.NIGHT)
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
                    plugin.Server.Map.SetIntercomContent(IntercomStatus.Ready,
                        $"READY\nSCP LEFT:{plugin.Server.Round.Stats.SCPAlive}/{plugin.Server.Round.Stats.SCPStart}\nCLASS-D LEFT:{plugin.Server.Round.Stats.ClassDAlive}/{plugin.Server.Round.Stats.ClassDStart}\nSCIENTIST LEFT:{plugin.Server.Round.Stats.ScientistsAlive}/{plugin.Server.Round.Stats.ScientistsStart}");
                }

                if (plugin.title_timer)
                {
                    string title = ConfigManager.Manager.Config.GetStringValue("player_list_title", "Unnamed Server") + " RoundTime: " + plugin.Server.Round.Duration / 60 + ":" + plugin.Server.Round.Duration % 60;
                    plugin.Server.PlayerListTitle = title;
                }

                if (plugin.traitor_limitter > 0)
                {
                    try
                    {
                        foreach (Player ply in plugin.Server.GetPlayers())
                        {
                            if ((ply.TeamRole.Role == Role.NTF_CADET ||
                                ply.TeamRole.Role == Role.NTF_LIEUTENANT ||
                                ply.TeamRole.Role == Role.NTF_SCIENTIST ||
                                ply.TeamRole.Role == Role.NTF_COMMANDER ||
                                ply.TeamRole.Role == Role.FACILITY_GUARD ||
                                ply.TeamRole.Role == Role.CHAOS_INSURGENCY) && ply.IsHandcuffed())
                            {
                                Vector pos = ply.GetPosition();
                                int ntfcount = plugin.Server.Round.Stats.NTFAlive;
                                int cicount = plugin.Server.Round.Stats.CiAlive;

                                plugin.Debug($"[TraitorCheck] NTF:{ntfcount} CI:{cicount} LIMIT:{plugin.traitor_limitter}");
                                plugin.Debug($"[TraitorCheck] {ply.Name}({ply.TeamRole.Role}) x:{pos.x} y:{pos.y} z:{pos.z} cuff:{ply.IsHandcuffed()}");

                                if ((pos.x >= 172 && pos.x <= 182) &&
                                    (pos.y >= 980 && pos.y <= 990) &&
                                    (pos.z >= 25 && pos.z <= 34))
                                {
                                    if ((ply.TeamRole.Role == Role.CHAOS_INSURGENCY && cicount <= plugin.traitor_limitter) ||
                                        (ply.TeamRole.Role != Role.CHAOS_INSURGENCY && ntfcount <= plugin.traitor_limitter))
                                    {
                                        int rndresult = rnd.Next(0, 100);
                                        plugin.Debug($"[TraitorCheck] Traitoring... [{ply.Name}:{ply.TeamRole.Role}:{rndresult}<={plugin.traitor_chance_percent}]");
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
                                            plugin.Info($"[TraitorCheck] Success [{ply.Name}:{ply.TeamRole.Role}:{rndresult}<={plugin.traitor_chance_percent}]");
                                        }
                                        else
                                        {
                                            ply.Teleport(traitor_pos, true);
                                            ply.Kill(DamageType.TESLA);
                                            plugin.Info($"[TraitorCheck] Failed [{ply.Name}:{ply.TeamRole.Role}:{rndresult}>={plugin.traitor_chance_percent}]");
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

        public void OnCallCommand(PlayerCallCommandEvent ev)
        {
            plugin.Debug($"[OnCallCommand] [Before] Called:{ev.Player.Name} Command:{ev.Command} Return:{ev.ReturnMessage}");

            int cooltime = -1;
            if (playeridlist.TryGetValue(ev.Player.PlayerId, out cooltime))
            {
                if (cooltime == 0)
                {
                    playeridlist[ev.Player.PlayerId] = 20;
                }
                else
                {
                    plugin.Debug($"[OnCallCommand] Command Rejected:{ev.Player.Name} CT:{cooltime}");
                    ev.ReturnMessage = "Command Rejected:使用間隔が短すぎます。";
                    return;
                }
            }
            else
            {
                playeridlist.Add(ev.Player.PlayerId, 30);
            }

            if (ev.ReturnMessage == "Command not found.")
            {
                if (ev.Command.StartsWith("help"))
                {
                    ev.ReturnMessage = "SanyaPlugin Command List\n.kill\n自殺します。\n\n.sinfo\nSCP時のみ仲間のSCPリストを表示します。\n\n.939sp\nSCP-939で放送が可能です。Vキーの人間向けにて使用可能。\n\n.079start\n.079stop\n核の起動/停止を行えます。(Tier3以上/AP125消費)\n\n.boost\nSCPの各種ブーストが使用可能。\n\n.attack\n079の場合カメラ射撃、人間の場合素手の時格闘が出来る。";
                }
                else if (ev.Command.StartsWith("kill"))
                {
                    if (roundduring)
                    {
                        plugin.Info($"[SelfKiller] {ev.Player.Name}");
                        if (ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR && ev.Player.TeamRole.Team != Smod2.API.Team.NONE)
                        {
                            ev.ReturnMessage = "Success.";
                            ev.Player.PersonalClearBroadcasts();
                            ev.Player.PersonalBroadcast(5, "<size=25>《あなたは自殺しました。》\n </size><size=20>《You suicided.》\n</size>", false);
                            ev.Player.SetGodmode(false);
                            ev.Player.Kill(DamageType.DECONT);
                        }
                        else
                        {
                            ev.ReturnMessage = "あなたは観戦状態です。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if (ev.Command.StartsWith("sinfo"))
                {
                    if (roundduring)
                    {
                        if (ev.Player.TeamRole.Team == Smod2.API.Team.SCP || ev.Player.GetBypassMode())
                        {
                            plugin.Info($"[SCPInfo] {ev.Player.Name}");

                            string scplist = "仲間のSCP情報：\n";
                            foreach (Player items in plugin.Server.GetPlayers().FindAll(fl => { return fl.TeamRole.Team == Smod2.API.Team.SCP; }))
                            {
                                scplist += $"{items.Name} : {items.TeamRole.Role}({items.GetHealth()}HP)\n";
                            }

                            ev.ReturnMessage = scplist;
                            ev.Player.PersonalClearBroadcasts();
                            ev.Player.PersonalBroadcast(10, $"<size=25>{scplist}</size>", false);
                        }
                        else
                        {
                            ev.ReturnMessage = "あなたはSCP陣営ではありません。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if (ev.Command.StartsWith("939sp"))
                {
                    if (roundduring)
                    {
                        if (ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                        {
                            plugin.Info($"[939Speaker] {ev.Player.Name}");

                            if (null == plugin.Server.Map.GetIntercomSpeaker())
                            {
                                plugin.Server.Map.SetIntercomSpeaker(ev.Player);
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(5, "<size=25>《放送を開始します。》\n </size><size=20>《You will broadcast.》\n</size>", false);
                                ev.ReturnMessage = "放送を開始します。";
                            }
                            else
                            {
                                if (ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                                {
                                    plugin.Server.Map.SetIntercomSpeaker(null);
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(5, "<size=25>《放送を終了しました。》\n </size><size=20>《You finished broadcasting.》\n</size>", false);
                                    ev.ReturnMessage = "放送を終了しました。";
                                }
                                else
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(5, "<size=25>《誰かが放送中です。》\n </size><size=20>《Someone is broadcasting.》\n</size>", false);
                                    ev.ReturnMessage = "誰かが放送中です。";
                                }
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = "あなたはSCP-939ではありません。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if (ev.Command.StartsWith("079nuke"))
                {
                    if (roundduring)
                    {
                        if (ev.Player.TeamRole.Role == Role.SCP_079)
                        {
                            plugin.Info($"[079nuke] {ev.Player.Name} [Tier:{ev.Player.Scp079Data.Level + 1} AP:{ev.Player.Scp079Data.AP}]");
                            if (ev.Player.Scp079Data.Level >= 2 && ev.Player.Scp079Data.AP >= 125)
                            {
                                if (!AlphaWarheadController.host.inProgress)
                                {
                                    AlphaWarheadController.host.InstantPrepare();
                                    AlphaWarheadController.host.StartDetonation(ev.Player.GetGameObject() as UnityEngine.GameObject);
                                    ev.ReturnMessage = "核操作（起動）成功。(AP-125)";
                                }
                                else
                                {
                                    AlphaWarheadController.host.CancelDetonation(ev.Player.GetGameObject() as UnityEngine.GameObject);
                                    ev.ReturnMessage = "核操作（停止）成功。(AP-125)";
                                }
                                ev.Player.Scp079Data.AP -= 125.0f;
                            }
                            else
                            {
                                ev.ReturnMessage = "核操作失敗。(APかTierが足りません)";
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = "あなたはSCP-079ではありません。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if (ev.Command.StartsWith("079sp"))
                {
                    if (roundduring)
                    {
                        if (ev.Player.TeamRole.Role == Role.SCP_079)
                        {
                            plugin.Info($"[079sp] {ev.Player.Name} [Tier:{ev.Player.Scp079Data.Level + 1} AP:{ev.Player.Scp079Data.AP}]");
                            if (plugin.Server.Map.GetIntercomSpeaker() != null)
                            {
                                plugin.Server.Map.SetIntercomSpeaker(null);
                                SanyaPlugin.CallAmbientSound((int)SANYA_AMBIENT_ID.SCP079);
                                ev.Player.Scp079Data.AP -= 5.0f;
                                ev.ReturnMessage = "割り込み成功。(AP-5)";
                            }
                            else
                            {
                                ev.ReturnMessage = "放送中ではありません。";
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = "あなたはSCP-079ではありません。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if (ev.Command.StartsWith("boost"))
                {
                    if (roundduring)
                    {
                        if (ev.Player.TeamRole.Role == Role.SCP_096)
                        {
                            Scp096PlayerScript ply096 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp096PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                            if (health > usehp && ply096 != null)
                            {
                                if (ply096.Networkenraged == Scp096PlayerScript.RageState.NotEnraged)
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(5, $"<size=25>《SCP-096ブーストを使用しました。(HP消費:{usehp})》\n </size><size=20>《Used <SCP-096> boost. (used HP:{usehp})》\n</size>", false);
                                    if (!ev.Player.GetBypassMode())
                                    {
                                        ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                    }
                                    ply096.IncreaseRage(1f);
                                    ev.ReturnMessage = $"Boost!(-{usehp})";
                                }
                                else
                                {
                                    ev.ReturnMessage = "クールタイム中です。";
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else if (ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                        {
                            Scp939PlayerScript ply939 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp939PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.1);

                            if (health > usehp && ply939 != null)
                            {
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(5, $"<size=25>《SCP-939ブーストを使用しました。(HP消費:{usehp})》\n </size><size=20>《Used <SCP-939> boost. (used HP:{usehp})》\n</size>", false);
                                if (!ev.Player.GetBypassMode())
                                {
                                    ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                }

                                List<Scp939_VisionController> list939 = new List<Scp939_VisionController>(UnityEngine.GameObject.FindObjectsOfType<Scp939_VisionController>()).FindAll(x => x.name.Contains("Player"));

                                plugin.Debug($"Vision:{list939.Count}");

                                foreach (Scp939_VisionController i in list939)
                                {
                                    i.MakeNoise(100f);
                                }

                                ply939.CallRpcShoot();

                                ev.ReturnMessage = $"Boost!(-{usehp})";
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else if (ev.Player.TeamRole.Role == Role.SCP_049)
                        {
                            Scp049PlayerScript ply049 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp049PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                            if (health > usehp && ply049 != null)
                            {
                                List<Ragdoll> rags = new List<Ragdoll>(GameObject.FindObjectsOfType<Ragdoll>());

                                plugin.Debug($"rags:{rags.Count}");

                                foreach (Ragdoll i in rags)
                                {
                                    plugin.Debug($"{i.owner.steamClientName}:{(Role)i.owner.charclass}");

                                    i.NetworkallowRecall = true;
                                    i.owner.deathCause.tool = (int)DamageType.SCP_049;
                                    foreach (Smod2.API.Player p in plugin.Server.GetPlayers())
                                    {
                                        if (p.PlayerId == i.owner.PlayerId)
                                        {
                                            p.Infect(60f);
                                        }
                                    }
                                }

                                if (!ev.Player.GetBypassMode())
                                {
                                    ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                }
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(5, $"<size=25>《SCP-049ブーストを使用しました。(HP消費:{usehp})》\n </size><size=20>《Used <SCP-049> boost. (used HP:{usehp})》\n</size>", false);
                                ev.ReturnMessage = $"Boost!(-{usehp})";
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else if (ev.Player.TeamRole.Role == Role.SCP_173)
                        {
                            Scp173PlayerScript ply173 = GameObject.Find("Host").GetComponent<Scp173PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int befhp = -1;
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                            if (health > usehp && ply173 != null)
                            {
                                if (!scp173_boosting && !ev.Player.GetGodmode())
                                {
                                    befhp = health;
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(5, $"<size=25>《SCP-173ブーストを使用しました。(10秒間)》\n </size><size=20>《Used <SCP-173> boost. (effective:10 seconds)》\n</size>", false);

                                    ev.Player.SetHealth(1, DamageType.NONE);
                                    ev.Player.SetGodmode(true);
                                    scp173_boosting = true;

                                    ev.ReturnMessage = $"Boost!(-{usehp})";

                                    System.Timers.Timer t = new System.Timers.Timer
                                    {
                                        Interval = 10000,
                                        AutoReset = false,
                                        Enabled = true
                                    };
                                    t.Elapsed += delegate
                                    {
                                        if (!ev.Player.GetBypassMode())
                                        {
                                            ev.Player.SetHealth(befhp - usehp, DamageType.NONE);
                                        }
                                        else
                                        {
                                            ev.Player.SetHealth(befhp, DamageType.NONE);
                                        }
                                        ev.Player.SetGodmode(false);
                                        scp173_boosting = false;

                                        if(plugin.Server.Map.WarheadDetonated &&  ev.Player.GetPosition().y < 900)
                                        {
                                            ev.Player.Kill(DamageType.NUKE);
                                        }

                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(5, $"<size=25>《SCP-173ブーストが終了。(HP:{befhp}-{usehp})》\n </size><size=20>《Ended <SCP-173> boost. (HP:{befhp}-{usehp})》\n</size>", false);
                                        t.Enabled = false;
                                    };
                                }
                                else
                                {
                                    ev.ReturnMessage = "ブースト中か、GodModeが有効です。";
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = "ブーストが可能なSCPではありません。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if (ev.Command.StartsWith("attack"))
                {
                    if (roundduring)
                    {
                        if (ev.Player.TeamRole.Role == Role.SCP_079)
                        {
                            UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
                            WeaponManager wpm = gameObject.GetComponent<WeaponManager>();
                            CharacterClassManager plychm = gameObject.GetComponent<CharacterClassManager>();
                            Scp079PlayerScript ply079 = gameObject.GetComponent<Scp079PlayerScript>();

                            if (gameObject != null && ply079 != null && ev.Player.Scp079Data.AP > 5)
                            {
                                RaycastHit raycastHit;
                                Vector3 forward = ply079.currentCamera.targetPosition.transform.forward;
                                Vector3 position = ply079.currentCamera.transform.position;

                                if (Physics.Raycast(position + forward, forward, out raycastHit, 500f, 1208246273))
                                {
                                    plugin.Debug("Hit");
                                    ServerMod2.API.SmodPlayer ply = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                    CharacterClassManager comp = raycastHit.transform.GetComponent<CharacterClassManager>();
                                    HitboxIdentity hitbox = raycastHit.collider.GetComponent<HitboxIdentity>();

                                    if (comp != null && ply != null && hitbox != null)
                                    {
                                        if (ply.TeamRole.Team != Smod2.API.Team.SCP)
                                        {
                                            plugin.Debug("Hurt");
                                            gameObject.GetComponent<PlayerStats>().HurtPlayer(
                                                new PlayerStats.HitInfo(
                                                    SanyaPlugin.HitboxDamageCalculate(hitbox, 5.0f),
                                                    gameObject.GetComponent<NicknameSync>().myNick + " (" + plychm.SteamId + ")",
                                                    DamageTypes.Tesla,
                                                    gameObject.GetComponent<QueryProcessor>().PlayerId)
                                                , raycastHit.transform.gameObject
                                            );
                                            raycastHit.transform.gameObject.GetComponent<FallDamage>().CallRpcDoSound(raycastHit.transform.position, 1.0f);
                                        }
                                    }
                                    wpm.CallRpcPlaceDecal(false, 0, raycastHit.point + raycastHit.normal * 0.01f, Quaternion.FromToRotation(Vector3.up, raycastHit.normal));
                                }

                                if (!ev.Player.GetBypassMode())
                                {
                                    ev.Player.Scp079Data.AP -= 5f;
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = "APが足りません。";
                            }

                            ev.ReturnMessage = "079 Attack!";
                        }
                        else if (ev.Player.GetCurrentItemIndex() == -1 && ev.Player.TeamRole.Team != Smod2.API.Team.SCP && ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR)
                        {
                            UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
                            CharacterClassManager plychm = gameObject.GetComponent<CharacterClassManager>();
                            Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                            Vector3 forward = scp049sc.plyCam.transform.forward;
                            Vector3 position = scp049sc.plyCam.transform.position;

                            forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

                            if (gameObject != null && forward != null && position != null)
                            {
                                RaycastHit raycastHit;
                                if (Physics.Raycast(position + forward, forward, out raycastHit, scp049sc.distance, 1208246273))
                                {
                                    plugin.Debug("Hit");
                                    ServerMod2.API.SmodPlayer ply = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                    CharacterClassManager comp = raycastHit.transform.GetComponent<CharacterClassManager>();
                                    HitboxIdentity hitbox = raycastHit.collider.GetComponent<HitboxIdentity>();

                                    if (comp != null && ply != null && hitbox != null)
                                    {
                                        if(ev.Player.PlayerId == ply.PlayerId)
                                        {
                                            plugin.Debug("Self");
                                        }
                                        else if (ev.Player.TeamRole.Team != ply.TeamRole.Team || friendlyfire)
                                        {
                                            plugin.Debug("Hurt");
                                            raycastHit.transform.gameObject.GetComponent<FallDamage>().CallRpcDoSound(raycastHit.transform.position, SanyaPlugin.HitboxDamageCalculate(hitbox, 1.0f));

                                            Vector3 newvec = forward;
                                            Vector pos = ply.GetPosition();
                                            newvec.Scale(new Vector3(0.25f, 0.25f, 0.25f));
                                            Vector targetpos = new Vector(pos.x + newvec.x, pos.y, pos.z + newvec.z);
                                            Vector3 targetpos_3 = new Vector3(pos.x + newvec.x, pos.y, pos.z + newvec.z);
                                            if (!raycastHit.transform.gameObject.GetComponent<PlyMovementSync>().IsStuck(targetpos_3))
                                            {
                                                ply.Teleport(targetpos);
                                            }

                                            gameObject.GetComponent<PlayerStats>().HurtPlayer(
                                                new PlayerStats.HitInfo(
                                                    SanyaPlugin.HitboxDamageCalculate(hitbox, 1.0f),
                                                    gameObject.GetComponent<NicknameSync>().myNick + " (" + plychm.SteamId + ")",
                                                    DamageTypes.None,
                                                    gameObject.GetComponent<QueryProcessor>().PlayerId)
                                                , raycastHit.transform.gameObject
                                            );
                                        }
                                    }
                                }
                            }
                            ev.ReturnMessage = "Punch!";
                        }
                        else
                        {
                            ev.ReturnMessage = "素手状態ではない、人間陣営ではない、またはSCP-079ではありません。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
            }

            plugin.Debug("[OnCallCommand] Called:" + ev.Player.Name + " Command:" + ev.Command + " Return:" + ev.ReturnMessage);
        }
    }
}
