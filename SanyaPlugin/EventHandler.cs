﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using MEC;
using Newtonsoft.Json;
using RemoteAdmin;
using Smod2;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;
using UnityEngine;


namespace SanyaPlugin
{
    public class Playerinfo
    {
        public Playerinfo() { }

        public string name { get; set; }

        public string steamid { get; set; }

        public string role { get; set; }
    }

    public class Serverinfo
    {
        public Serverinfo() { players = new List<Playerinfo>(); }

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

    public class SCPPlayerData
    {
        public SCPPlayerData(int i, string n, Role r, Vector p, int h, int l079 = -1, int a079 = -1, Vector c079 = null) { id = i; name = n; role = r; pos = p; health = h; level079 = l079; ap079 = a079; camera079 = c079; }

        public int id { get; set; }

        public string name { get; set; }

        public Role role { get; set; }

        public Vector pos { get; set; }

        public int health { get; set; }

        public int level079 { get; set; }

        public int ap079 { get; set; }

        public Vector camera079 { get; set; }
    }

    public class EventHandler :
        IEventHandlerWaitingForPlayers,
        IEventHandlerRoundStart,
        IEventHandlerRoundEnd,
        IEventHandlerCheckRoundEnd,
        IEventHandlerRoundRestart,
        IEventHandlerPlayerJoin,
        IEventHandlerDisconnect,
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
        IEventHandlerGeneratorFinish,
        IEventHandler079Door,
        IEventHandler079Elevator,
        IEventHandler079StartSpeaker,
        IEventHandler079Lockdown,
        IEventHandlerLure,
        IEventHandlerSummonVehicle,
        IEventHandlerShoot,
        IEventHandlerTeamRespawn,
        IEventHandlerInitialAssignTeam,
        IEventHandlerWarheadChangeLever,
        IEventHandlerWarheadKeycardAccess,
        IEventHandlerIntercom,
        IEventHandlerGrenadeHitPlayer,
        IEventHandlerUpdate
    {
        private readonly SanyaPlugin plugin;
        UdpClient udpclient = new UdpClient();
        CoroutineHandle infosender;
        private bool running = false;

        //-----------------var---------------------
        //GlobalStatus
        private bool roundduring = false;
        private System.Random rnd = new System.Random();
        private bool friendlyfire = false;
        public List<SCPPlayerData> scplist = new List<SCPPlayerData>();
        private int autowarheadtime = -1;

        //Eventer
        private SANYA_GAME_MODE eventmode = SANYA_GAME_MODE.NULL;
        //--HCZ--
        private List<Room> hcz_hallways = new List<Room>();
        private int fgamount = 0;
        private bool storyshaker = false;
        private int startwait = -1;
        //--CLASSDINS---
        private Vector armory = null;
        //--STORY--
        private int chamber173count = 0;
        private int hall173count = 0;
        private Vector cam173hall = null;
        private Vector cam173chamber = null;
        private int scp173amount = 0;
        private int scp079amount = 0;

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

        //106Lure
        private int lure_id = -1;

        //Dot
        static internal List<Player> dot_target = new List<Player>();

        //Update
        private int updatecounter = 0;
        private Vector traitor_pos = new Vector(170, 984, 28);
        private float decont_time = 0;

        //CommandCoolTime
        private Dictionary<int, int> playeridlist = new Dictionary<int, int>();

        //Command
        private bool scp173_boosting = false;
        static internal bool scp939_53_boosting = false;
        static internal bool scp939_89_boosting = false;
        private int scp079_boost_cooltime = 0;
        private int scp079_attack_cooltime = 0;

        //-----------------------Event---------------------
        public EventHandler(SanyaPlugin plugin)
        {
            this.plugin = plugin;
        }

        private IEnumerator<float> Sender()
        {
            while(true)
            {
                try
                {
                    string ip = plugin.info_sender_to_ip;
                    int port = plugin.info_sender_to_port;

                    if(ip == "none")
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

                    if(cinfo.playing > 0)
                    {
                        foreach(Player player in server.GetPlayers())
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
                }
                catch(Exception e)
                {
                    plugin.Error($"[Infosender] {e.ToString()}");
                    yield break;
                }
                yield return Timing.WaitForSeconds(15f);
            }
        }

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            running = true;
            Timing.KillCoroutines("InfoSender");
            infosender = Timing.RunCoroutine(this.Sender(), Segment.Update, "InfoSender");

            SanyaPlugin.roundStartTime = System.DateTime.Now;
            CharacterClassManager.smRoundStartTime = 0f;
            SanyaPlugin.SetExtraDoorNames();
            if(plugin.nuke_button_auto_close > 0f)
            {
                SanyaPlugin.SetExtraPermission("NUKE_SURFACE", "EXIT_ACC");
            }
            roundduring = false;

            genperm = new List<string>(plugin.ConfigManager.Config.GetListValue("generator_keycard_perm", new string[] { "ARMORY_LVL_3" }, false));
            decont_time = plugin.ConfigManager.Config.GetFloatValue("decontamination_time", 11.74f);
            friendlyfire = plugin.ConfigManager.Config.GetBoolValue("friendly_fire", false);
            startwait = plugin.ConfigManager.Config.GetIntValue("173_door_starting_cooldown", 25) * 3;
            autowarheadtime = plugin.ConfigManager.Config.GetIntValue("auto_warhead_start", -1);

            eventmode = (SANYA_GAME_MODE)SanyaPlugin.GetRandomIndexFromWeight(plugin.event_mode_weight);
            switch(eventmode)
            {
                case SANYA_GAME_MODE.NULL:
                    eventmode = SANYA_GAME_MODE.NORMAL;
                    break;
                case SANYA_GAME_MODE.STORY:
                    foreach(Camera079 item in Scp079PlayerScript.allCameras)
                    {
                        if(item.cameraName.StartsWith("173 HALLWAY"))
                        {
                            cam173hall = new Vector(item.transform.position.x, item.transform.position.y - 2, item.transform.position.z);
                        }
                        else if(item.cameraName.StartsWith("173 CHAMBER"))
                        {
                            cam173chamber = new Vector(item.transform.position.x, item.transform.position.y - 1, item.transform.position.z);
                        }
                    }
                    break;
                case SANYA_GAME_MODE.CLASSD_INSURGENCY:
                    foreach(Room item in plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.SPEAKER))
                    {
                        if(item.RoomType == RoomType.LCZ_ARMORY)
                        {
                            armory = new Vector(item.Position.x, item.Position.y + 2, item.Position.z);
                        }
                    }
                    break;
                case SANYA_GAME_MODE.HCZ:
                    foreach(Room item in plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA))
                    {
                        if(item.ZoneType == ZoneType.HCZ && (item.RoomType == RoomType.X_INTERSECTION || item.RoomType == RoomType.T_INTERSECTION || item.RoomType == RoomType.STRAIGHT))
                        {
                            hcz_hallways.Add(item);
                        }
                    }
                    break;
            }
            plugin.Info($"[RandomEventer] Selected:{eventmode.ToString()}");
            if(eventmode == SANYA_GAME_MODE.NO_SCP)
            {
                plugin.Warn($"Warning:NO_SCP MODE SELECTED.");
            }
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            updatecounter = 0;
            SanyaPlugin.roundStartTime = System.DateTime.Now;

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

            if(eventmode != SANYA_GAME_MODE.STORY)
            {
                if(plugin.classd_startitem_percent > 0)
                {
                    int success_count = 0;
                    foreach(Vector spawnpos in plugin.Server.Map.GetSpawnPoints(Role.CLASSD))
                    {
                        int percent = rnd.Next(0, 100);
                        ItemType ritem;

                        if(percent <= plugin.classd_startitem_percent)
                        {
                            success_count++;
                            ritem = (ItemType)plugin.classd_startitem_ok_itemid;
                        }
                        else
                        {
                            ritem = (ItemType)plugin.classd_startitem_no_itemid;
                        }

                        if(ritem >= 0)
                        {
                            plugin.Server.Map.SpawnItem((ItemType)ritem, spawnpos, new Vector(0, 0, 0));
                        }
                    }
                    plugin.Info($"Class-D-Contaiment Item Droped! (Success:{success_count})");
                }
            }


            if(eventmode == SANYA_GAME_MODE.CLASSD_INSURGENCY)
            {
                foreach(Smod2.API.Item i in plugin.Server.Map.GetItems(ItemType.MP4, true))
                {
                    Vector v = i.GetPosition();
                    for(int x = 0; x < plugin.classd_ins_items; x++)
                    {
                        plugin.Server.Map.SpawnItem(ItemType.MP4, new Vector(v.x, v.y + 1, v.z), new Vector(0, 0, 0));
                    }
                }

                //mod
                foreach(var i in plugin.Server.Map.GetItems(ItemType.MP4, true))
                {
                    Pickup pick = i.GetComponent() as Pickup;
                    pick.RefreshDurability(true, true);
                }

                foreach(Smod2.API.Item i in plugin.Server.Map.GetItems(ItemType.P90, true))
                {
                    Vector v = i.GetPosition();
                    for(int x = 0; x < plugin.classd_ins_items; x++)
                    {
                        plugin.Server.Map.SpawnItem(ItemType.CHAOS_INSURGENCY_DEVICE, new Vector(v.x, v.y + 1, v.z), new Vector(0, 0, 0));
                    }
                }

                foreach(Room r in plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.SPEAKER))
                {
                    if(r.RoomType == RoomType.LCZ_ARMORY)
                    {
                        Vector v = r.SpeakerPosition;
                        for(int x = 0; x < plugin.classd_ins_items; x++)
                        {
                            plugin.Server.Map.SpawnItem(ItemType.DISARMER, new Vector(v.x, v.y - 1, v.z), new Vector(0, 0, 0));
                        }
                    }
                }
            }
            else if(eventmode == SANYA_GAME_MODE.STORY)
            {
                foreach(Smod2.API.Player i in plugin.Server.GetPlayers())
                {
                    if(i.TeamRole.Team == Smod2.API.Team.SCP)
                    {
                        foreach(var x in plugin.Server.GetRoles())
                        {
                            if(x.Role == i.TeamRole.Role)
                            {
                                x.RoleDisallowed = false;
                            }
                        }
                        i.ChangeRole(Role.TUTORIAL);
                    }
                }
            }
            else if(eventmode == SANYA_GAME_MODE.NO_SCP)
            {
                SanyaPlugin.CloseBlastDoor();

                RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                foreach(var i in plugin.Server.Map.GetItems(ItemType.E11_STANDARD_RIFLE, true))
                {
                    Vector pos = new Vector(i.GetPosition().x, i.GetPosition().y + 1, i.GetPosition().z);
                    plugin.Server.Map.SpawnItem(ItemType.P90, pos, new Vector(0f, 0f, 0f));
                    i.Remove();
                }
                foreach(var i in plugin.Server.Map.GetItems(ItemType.MICROHID, true))
                {
                    Vector pos = new Vector(i.GetPosition().x, i.GetPosition().y + 1, i.GetPosition().z);
                    plugin.Server.Map.SpawnItem(ItemType.E11_STANDARD_RIFLE, pos, new Vector(0f, 0f, 0f));
                    i.Remove();
                }
                foreach(var i in plugin.Server.Map.GetItems(ItemType.DROPPED_5, true))
                {
                    Vector pos = new Vector(i.GetPosition().x, i.GetPosition().y + 1, i.GetPosition().z);
                    plugin.Server.Map.SpawnItem(ItemType.LOGICER, pos, new Vector(0f, 0f, 0f));
                    i.Remove();
                }

                //mod
                foreach(var i in plugin.Server.Map.GetItems(ItemType.E11_STANDARD_RIFLE, true))
                {
                    Pickup pick = i.GetComponent() as Pickup;
                    pick.RefreshDurability(true, true);
                }
                foreach(var i in plugin.Server.Map.GetItems(ItemType.P90, true))
                {
                    Pickup pick = i.GetComponent() as Pickup;
                    pick.RefreshDurability(true, true);
                }
            }
        }

        public void OnCheckRoundEnd(CheckRoundEndEvent ev)
        {
            if(plugin.summary_less_mode)
            {
                if(tempStatus != ev.Status && !isEnded)
                {
                    if(tempStatus == ROUND_END_STATUS.ON_GOING)
                    {
                        plugin.Info($"Round Ended [NonSummary-Mode] [{ev.Status}]");
                        plugin.Info($"Class-D:{ev.Round.Stats.ClassDAlive} Scientist:{ev.Round.Stats.ScientistsAlive} NTF:{ev.Round.Stats.NTFAlive} SCP:{ev.Round.Stats.SCPAlive} CI:{ev.Round.Stats.CiAlive}");

                        UnityEngine.GameObject.Find("Host").GetComponent<MTFRespawn>().SetDecontCooldown(60f);
                        if(plugin.endround_all_godmode)
                        {
                            foreach(Player item in plugin.Server.GetPlayers())
                            {
                                item.SetGodmode(true);
                            }
                        }
                    }
                    tempStatus = ev.Status;
                }

                if(ev.Status != ROUND_END_STATUS.ON_GOING)
                {
                    ev.Status = ROUND_END_STATUS.ON_GOING;
                    isEnded = true;
                }
            }
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if(roundduring)
            {
                plugin.Info($"Round Ended [{ev.Status}]");
                plugin.Info($"Class-D:{ev.Round.Stats.ClassDAlive} Scientist:{ev.Round.Stats.ScientistsAlive} NTF:{ev.Round.Stats.NTFAlive} SCP:{ev.Round.Stats.SCPAlive} CI:{ev.Round.Stats.CiAlive}");

                if(plugin.endround_all_godmode)
                {
                    foreach(Player item in plugin.Server.GetPlayers())
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
            scplist.Clear();
            gencomplete = false;
            lure_id = -1;
            chamber173count = 0;
            hall173count = 0;
            armory = null;
            cam173hall = null;
            cam173chamber = null;
            storyshaker = false;
            scp173amount = 0;
            scp079amount = 0;
            fgamount = 0;
            SanyaPlugin.scp_override_steamid = "";
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            plugin.Info($"[PlayerJoin] {ev.Player.Name}[{ev.Player.IpAddress}]({ev.Player.SteamId})");

            //scplist
            if(plugin.scp_disconnect_at_resetrole)
            {
                var target = scplist.Find(x => x.name == ev.Player.Name);
                if(target != null)
                {
                    plugin.Warn($"[SCPList/ReSet] {target.name}:{target.role} ({target.health}/{target.level079}/{target.ap079})");
                    target.id = ev.Player.PlayerId;
                    ev.Player.PersonalClearBroadcasts();
                    ev.Player.PersonalBroadcast(3, $"<size=30>切断前のSCPに復帰します。</size>",false);
                    Timing.RunCoroutine(SanyaPlugin._DelayedSetReSetRole(target, ev.Player), Segment.Update);
                }
            }
        }

        public void OnDisconnect(DisconnectEvent ev)
        {
            plugin.Debug($"[OnDisconnect]");

            if(plugin.scp_disconnect_at_resetrole)
            {
                if(scplist.Count > 0)
                {
                    foreach(var ply in plugin.Server.GetPlayers())
                    {
                        if(ply.TeamRole.Team == Smod2.API.Team.SCP)
                        {
                            var target = scplist.Find(x => x.name == ply.Name);
                            if(target != null)
                            {
                                target.health = ply.GetHealth();
                                target.pos = ply.GetPosition();
                                target.role = ply.TeamRole.Role;
                                if(ply.TeamRole.Role == Role.SCP_079)
                                {
                                    target.level079 = ply.Scp079Data.Level;
                                    target.ap079 = (int)ply.Scp079Data.AP;
                                    target.camera079 = ply.Scp079Data.Camera;
                                }
                                plugin.Debug($"[SCPList_Save] [{target.id}]{target.name}:{target.role}:{target.pos}:{target.health}:{target.level079}:{target.ap079}:{target.camera079}");
                            }
                        }
                    }
                }
            }
        }

        public void OnDecontaminate()
        {
            plugin.Info($"LCZ Decontaminated");

            if(plugin.cassie_subtitle && roundduring)
            {
                plugin.Server.Map.ClearBroadcasts();
                plugin.Server.Map.Broadcast(13, $"<size=25>《下層がロックされ、「再収容プロトコル」の準備が出来ました。全ての有機物は破壊されます。》\n </size><size=20>《Light Containment Zone is locked down and ready for decontamination. The removal of organic substances has now begun.》\n</size>", false);
            }
        }

        public void OnDetonate()
        {
            plugin.Info($"AlphaWarhead Denotated");


            List<Vector> explode_at = new List<Vector>();

            explode_at.Add(new Vector(-16, 1001, 0.5f));
            explode_at.Add(new Vector(87.5f, 994, -44));
            explode_at.Add(new Vector(5, 1001, 7));
            explode_at.Add(new Vector(-5, 1001, 7));
            explode_at.Add(new Vector(20, 1001, -34));
            explode_at.Add(new Vector(40, 1001, -34));


            GrenadeManager gm = GameObject.Find("Host").GetComponent<GrenadeManager>();
            foreach(var pos in explode_at)
            {
                string gid = "SERVER_" + -1 + ":" + (gm.smThrowInteger + 4096);
                gm.CallRpcThrowGrenade(0, -1, gm.smThrowInteger++ + 4096, new Vector3(0f, 0f, 0f), true, new Vector3(0, 0, 0), false, 0);
                gm.CallRpcUpdate(gid, new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(Vector3.zero), Vector3.zero, Vector3.zero);
                gm.CallRpcExplode(gid, -1);
                plugin.Debug($"NukeExplode:{pos}");
            }
        }

        public void OnStartCountdown(WarheadStartEvent ev)
        {
            plugin.Debug($"[OnStartCountdown] {ev.Activator?.Name}:{ev.Cancel}:{ev.TimeLeft}");
            int autoleft = (int)Math.Truncate(AlphaWarheadController.host.smtimeToScheduledDetonation);

            if(autoleft == 0 && autowarheadtime != -1)
            {
                AlphaWarheadController.host.Networksync_resumeScenario = 4;
                AlphaWarheadController.host.InstantPrepare();
                AlphaWarheadController.host.NetworkinProgress = true;
                ev.IsResumed = true;
                ev.TimeLeft = 60f;
            }

            if(!ev.Cancel && AlphaWarheadController.host.inProgress)
            {
                if(plugin.nuke_start_countdown_door_lock)
                {
                    foreach(Smod2.API.Door door in plugin.Server.Map.GetDoors())
                    {
                        if(autoleft == 0 && autowarheadtime != -1)
                        {
                            door.Open = true;
                            door.Locked = true;
                        }
                        else
                        {
                            if(!door.Name.Contains("GATE_") && !door.Name.Contains("106_"))
                            {
                                door.Open = true;
                                door.Locked = true;
                            }
                        }
                    }
                }

                if(plugin.cassie_subtitle && roundduring)
                {
                    plugin.Server.Map.ClearBroadcasts();
                    if(!ev.IsResumed)
                    {
                        if(ev.Activator != null)
                        {
                            plugin.Server.Map.Broadcast(15, $"<color=#ff0000><size=25>《[{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]により「AlphaWarhead」の緊急起爆シーケンスが開始されました。\n施設の地下区画は、約90秒後に爆破されます。》\n</size><size=20>《Alpha Warhead emergency detonation sequence engaged by [{ev.Activator.Name}/{ev.Activator.TeamRole.Team}].\nThe underground section of the facility will be detonated in t-minus 90 seconds.》\n</size></color>", false);
                        }
                        else
                        {
                            if(plugin.original_auto_nuke)
                            {
                                if(isLocked)
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
                        if(ev.Activator != null)
                        {
                            plugin.Server.Map.Broadcast(10, $"<color=#ff0000><size=25>《[{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]により緊急起爆シーケンスが再開されました。約{count.ToString()}秒後に爆破されます。》\n</size><size=20>《Detonation sequence resumed by [{ev.Activator.Name}/{ev.Activator.TeamRole.Team}]. t-minus {count.ToString()} seconds.》\n</size></color>", false);
                        }
                        else
                        {
                            if(plugin.original_auto_nuke)
                            {
                                if(isLocked)
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
            plugin.Debug($"[OnStopCountdown] {ev.Activator?.Name}:{ev.Cancel}:{ev.TimeLeft}");

            if(!ev.Cancel)
            {
                AlphaWarheadOutsitePanel.nukeside.SetEnabled(false);

                if(plugin.nuke_start_countdown_door_lock)
                {
                    foreach(Smod2.API.Door door in plugin.Server.Map.GetDoors())
                    {
                        if(!door.Name.Contains("GATE_") && !door.Name.Contains("106_"))
                        {
                            door.Locked = false;
                        }
                    }
                }

                if(plugin.cassie_subtitle && roundduring)
                {
                    plugin.Server.Map.ClearBroadcasts();
                    if(ev.Activator != null)
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

        public void OnChangeLever(WarheadChangeLeverEvent ev)
        {
            plugin.Debug($"[OnChangeLever] {ev.Player.Name}:{ev.Allow}:{AlphaWarheadOutsitePanel.nukeside.Networkenabled}");

            if(eventmode == SANYA_GAME_MODE.NO_SCP)
            {
                if(AlphaWarheadOutsitePanel.nukeside.Networkenabled)
                {
                    ev.Allow = false;
                }
                else
                {
                    SanyaPlugin.CallAlphaWarhead(ev.Player);
                }
            }
        }

        public void OnWarheadKeycardAccess(WarheadKeycardAccessEvent ev)
        {
            plugin.Debug($"[OnWarheadKeycardAccess] {ev.Player.Name}:{ev.Allow}");

            if(ev.Allow && plugin.nuke_button_auto_close > 0f)
            {
                plugin.Debug($"[OnWarheadKeycardAccess] RunCoroutine:DeleyadCloseOutsideWarHeadCap delay:{plugin.nuke_button_auto_close}");
                Timing.RunCoroutine(SanyaPlugin._DeleyadCloseOutsideWarHeadCap(plugin.nuke_button_auto_close), Segment.Update);
            }
        }

        public void OnSetNTFUnitName(SetNTFUnitNameEvent ev)
        {
            if(roundduring)
            {
                if(plugin.cassie_subtitle)
                {
                    plugin.Server.Map.ClearBroadcasts();
                    if(plugin.Server.Round.Stats.SCPAlive > 0)
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

            if(!roundduring)
            {
                ev.AllowEscape = false;
                return;
            }

            if(plugin.escape_spawn)
            {
                plugin.Info($"[OnCheckEscape] Escaped:{ev.Player.Name}");
                (ev.Player.GetGameObject() as GameObject).GetComponent<Inventory>().ServerDropAll();
                if((ev.Player.TeamRole.Role == Role.CLASSD && ev.Player.IsHandcuffed()) || (ev.Player.TeamRole.Role == Role.SCIENTIST && !ev.Player.IsHandcuffed()))
                {
                    plugin.Debug($"Call Teleport");
                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, ev.Player.GetPosition(), false), Segment.Update);
                }
            }
        }

        public void OnAssignTeam(PlayerInitialAssignTeamEvent ev)
        {
            plugin.Debug($"[OnAssignTeam] {ev.Player.Name}:{ev.Team}");

            if(SanyaPlugin.scp_override_steamid.Length > 0)
            {
                if(ev.Player.SteamId == SanyaPlugin.scp_override_steamid)
                {
                    ev.Team = Smod2.API.Team.SCP;
                    plugin.Info($"[SCPOverRide] {ev.Player.Name}");
                }
            }

            if(eventmode == SANYA_GAME_MODE.NO_SCP)
            {
                if(ev.Team == Smod2.API.Team.SCP)
                {
                    ev.Team = Smod2.API.Team.CHAOS_INSURGENCY;
                }
            }
            else if(eventmode == SANYA_GAME_MODE.HCZ)
            {
                if((ev.Team == Smod2.API.Team.NINETAILFOX || ev.Team == Smod2.API.Team.CHAOS_INSURGENCY) && fgamount < plugin.hczstart_mtf_and_ci)
                {
                    fgamount++;
                    plugin.Info($"[OnAssignTeam] {ev.Player.Name} {ev.Team}->SCIENTIST({eventmode})");
                    ev.Team = Smod2.API.Team.SCIENTIST;
                }
            }
        }

        public void OnSetRole(PlayerSetRoleEvent ev)
        {
            plugin.Debug($"[OnSetRole] {ev.Player.Name}:{ev.Role}");

            //scplist
            if(plugin.scp_disconnect_at_resetrole)
            {
                var target = scplist.Find(x => x.name == ev.Player.Name);
                if(target == null && ev.TeamRole.Team == Smod2.API.Team.SCP)
                {
                    plugin.Warn($"[SCPList/Add] {ev.Player.Name}:{ev.Role}");
                    scplist.Add(new SCPPlayerData(ev.Player.PlayerId, ev.Player.Name, ev.Role, ev.Player.GetPosition(), ev.Player.TeamRole.MaxHP));
                }
                else if(target != null && ev.TeamRole.Team != Smod2.API.Team.SCP && ev.TeamRole.Team != Smod2.API.Team.TUTORIAL && target.id == ev.Player.PlayerId)
                {
                    plugin.Warn($"[SCPList/Remove] {target.name}:{target.role}");
                    scplist.Remove(target);
                }
            }

            //------------------------------------EventMode/SetRole---------------------------
            if(eventmode == SANYA_GAME_MODE.STORY)
            {
                if(cam173chamber != null && ev.Role == Role.CLASSD && chamber173count < 4)
                {
                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, cam173chamber, false), Segment.Update);
                    chamber173count++;
                }

                if(cam173hall != null && ev.Role == Role.SCIENTIST && hall173count < 4)
                {
                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, cam173hall, false), Segment.Update);
                    hall173count++;
                }

                if(ev.Role == Role.TUTORIAL)
                {
                    if(scp173amount == 0)
                    {
                        plugin.Info($"[Eventer:STORY] SCP-173:{ev.Player.Name}");
                        ev.Role = Role.SCP_173;
                        scp173amount++;
                    }
                    else if(scp079amount == 0)
                    {
                        plugin.Info($"[Eventer:STORY] SCP-079:{ev.Player.Name}");
                        ev.Role = Role.SCP_079;
                        scp079amount++;
                    }
                    else
                    {
                        List<Role> scpqueue = new List<Role>();
                        foreach(var i in plugin.Server.GetRoles("SCP"))
                        {
                            if(!i.RoleDisallowed && i.Role != Role.SCP_173 && i.Role != Role.SCP_079)
                            {
                                plugin.Debug($"add queue:{i.Role}");
                                scpqueue.Add(i.Role);
                            }
                        }
                        if(scpqueue.Count != 0)
                        {
                            ev.Role = scpqueue[rnd.Next(0, scpqueue.Count)];
                            plugin.Server.GetRoles("SCP").Find(x => x.Role == ev.Role).RoleDisallowed = true;
                            plugin.Info($"[Eventer:STORY] OtherSCP({ev.Role}):{ev.Player.Name}");
                        }
                        else
                        {
                            plugin.Error($"[Eventer:STORY] No SCP Queues,Skipped...({ev.Role}):{ev.Player.Name}");
                        }


                        //Other SCP = 372CONTAIN
                        Vector containpos = null;
                        RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                        foreach(var pos in rnde.posIds)
                        {
                            if(pos.posID == "RandomPistol" && pos.position.position.y > 0.5f && pos.position.position.y < 0.7f)
                            {
                                containpos = new Vector(pos.position.position.x, pos.position.position.y + 1, pos.position.position.z);
                            }
                        }
                        if(containpos != null)
                        {
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, containpos, false), Segment.Update);
                        }
                    }
                }
            }
            else if(eventmode == SANYA_GAME_MODE.CLASSD_INSURGENCY)
            {
                if(armory != null && ev.Role == Role.CLASSD)
                {
                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, armory, false), Segment.Update);
                }
            }
            else if(eventmode == SANYA_GAME_MODE.HCZ)
            {
                Room room = hcz_hallways[rnd.Next(0, hcz_hallways.Count)];
                Vector pos = new Vector(room.Position.x, room.Position.y + 3, room.Position.z);

                if(hcz_hallways != null && (ev.Role == Role.CLASSD || ev.Role == Role.SCIENTIST))
                {
                    plugin.Debug($"HCZ Teleport:{room.RoomType} : {pos}");
                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, pos, false), Segment.Update);
                }
            }
            else if(eventmode == SANYA_GAME_MODE.NO_SCP)
            {
                switch(ev.Role)
                {
                    case Role.NTF_CADET:
                    case Role.NTF_SCIENTIST:
                    case Role.NTF_LIEUTENANT:
                    case Role.NTF_COMMANDER:
                    case Role.FACILITY_GUARD:
                        ev.UsingDefaultItem = false;
                        if(ev.Items.RemoveAll(x => x == ItemType.MP4) > 0)
                        {
                            ev.Items.Insert(0, ItemType.P90);
                        }
                        if(ev.Items.RemoveAll(x => x == ItemType.E11_STANDARD_RIFLE) > 0)
                        {
                            ev.Items.Insert(0, ItemType.P90);
                        }
                        break;
                    case Role.CHAOS_INSURGENCY:
                        ev.UsingDefaultItem = false;
                        if(ev.Items.RemoveAll(x => x == ItemType.LOGICER) > 0)
                        {
                            ev.Items.Insert(0, ItemType.MP4);
                        }
                        if(plugin.Round.Duration < 10)
                        {
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, SanyaPlugin.GetSpawnElevatorPos(plugin.Server.Map.GetElevators().ToArray(), true, false), false), Segment.Update);
                        }
                        break;
                }
            }

            //---------------------DefaultAmmo---------------------
            int[] targetammo = new int[] { 0, 0, 0 };
            switch(ev.Role)
            {
                case Role.CLASSD:
                    targetammo = plugin.default_ammo_classd;
                    if(eventmode == SANYA_GAME_MODE.CLASSD_INSURGENCY)
                    {
                        targetammo[(int)AmmoType.DROPPED_7] += 35 * 4;
                    }
                    break;
                case Role.SCIENTIST:
                    targetammo = plugin.default_ammo_scientist;
                    break;
                case Role.FACILITY_GUARD:
                    targetammo = plugin.default_ammo_guard;
                    if(eventmode == SANYA_GAME_MODE.NO_SCP)
                    {
                        targetammo[(int)AmmoType.DROPPED_5] = 0;
                        targetammo[(int)AmmoType.DROPPED_7] = 0;
                        targetammo[(int)AmmoType.DROPPED_9] = 250;
                    }
                    break;
                case Role.CHAOS_INSURGENCY:
                    targetammo = plugin.default_ammo_ci;
                    if(eventmode == SANYA_GAME_MODE.NO_SCP)
                    {
                        targetammo[(int)AmmoType.DROPPED_5] = 0;
                        targetammo[(int)AmmoType.DROPPED_7] = 175;
                        targetammo[(int)AmmoType.DROPPED_9] = 0;
                    }
                    break;
                case Role.NTF_SCIENTIST:
                    targetammo = plugin.default_ammo_ntfscientist;
                    if(eventmode == SANYA_GAME_MODE.NO_SCP)
                    {
                        targetammo[(int)AmmoType.DROPPED_5] = 0;
                        targetammo[(int)AmmoType.DROPPED_7] = 0;
                        targetammo[(int)AmmoType.DROPPED_9] = 300;
                    }
                    break;
                case Role.NTF_LIEUTENANT:
                    targetammo = plugin.default_ammo_lieutenant;
                    if(eventmode == SANYA_GAME_MODE.NO_SCP)
                    {
                        targetammo[(int)AmmoType.DROPPED_5] = 0;
                        targetammo[(int)AmmoType.DROPPED_7] = 0;
                        targetammo[(int)AmmoType.DROPPED_9] = 250;
                    }
                    break;
                case Role.NTF_COMMANDER:
                    targetammo = plugin.default_ammo_commander;
                    if(eventmode == SANYA_GAME_MODE.NO_SCP)
                    {
                        targetammo[(int)AmmoType.DROPPED_5] = 0;
                        targetammo[(int)AmmoType.DROPPED_7] = 0;
                        targetammo[(int)AmmoType.DROPPED_9] = 300;
                    }
                    break;
                case Role.NTF_CADET:
                    targetammo = plugin.default_ammo_cadet;
                    if(eventmode == SANYA_GAME_MODE.NO_SCP)
                    {
                        targetammo[(int)AmmoType.DROPPED_5] = 0;
                        targetammo[(int)AmmoType.DROPPED_7] = 0;
                        targetammo[(int)AmmoType.DROPPED_9] = 250;
                    }
                    break;
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
            if(plugin.scp106_lure_speaktime > 0 && roundduring)
            {
                if(ev.DamageType == DamageType.LURE && lure_id == -1)
                {
                    if(plugin.Server.Map.GetIntercomSpeaker() == null)
                    {
                        plugin.Info($"[106Lure] Contained({ev.Player.Name}):Start Speaking...({plugin.scp106_lure_speaktime}seconds)");
                        ev.Damage = 0.0f;
                        lure_id = ev.Player.PlayerId;
                        ev.Player.SetGodmode(true);
                        foreach(Smod2.API.Item hasitem in ev.Player.GetInventory())
                        {
                            hasitem.Remove();
                        }
                        ev.Player.PersonalClearBroadcasts();
                        ev.Player.PersonalBroadcast((uint)plugin.scp106_lure_speaktime, $"<color=#ffff00><size=25>《あなたはSCP-106の再収容に使用されます。最後に{plugin.scp106_lure_speaktime}秒の放送時間が与えられます。》\n</size><size=20>《You will be used for recontainment SCP-106. You can broadcast for {plugin.scp106_lure_speaktime} seconds.》\n</size></color>", false);

                        plugin.Server.Map.SetIntercomSpeaker(ev.Player);

                        Timing.RunCoroutine(SanyaPlugin._LureIntercom(ev.Player, plugin), Segment.Update);
                    }
                    else
                    {
                        ev.Damage = 0.0f;
                    }
                }
            }

            if(plugin.friendly_warn || plugin.friendly_warn_console)
            {
                if(ev.Attacker != null &&
                    ev.DamageType != DamageType.NONE &&
                    ev.Attacker.Name != "Server" &&
                    (ev.Attacker.TeamRole.Team == ev.Player.TeamRole.Team 
                    || ev.Attacker.TeamRole.Team == Smod2.API.Team.NINETAILFOX && ev.Player.TeamRole.Team == Smod2.API.Team.SCIENTIST 
                    || ev.Attacker.TeamRole.Team == Smod2.API.Team.CLASSD && ev.Player.TeamRole.Team == Smod2.API.Team.CHAOS_INSURGENCY
                    || ev.Attacker.TeamRole.Team == Smod2.API.Team.SCIENTIST && ev.Player.TeamRole.Team == Smod2.API.Team.NINETAILFOX
                    || ev.Attacker.TeamRole.Team == Smod2.API.Team.CHAOS_INSURGENCY && ev.Player.TeamRole.Team == Smod2.API.Team.CLASSD
                    ) &&
                    ev.Attacker.PlayerId != ev.Player.PlayerId &&
                    ev.Player.GetGodmode() == false &&
                    roundduring)
                {
                    plugin.Info($"[FriendlyFire] {ev.Attacker.Name}({ev.DamageType}:{ev.Damage}) -> {ev.Player.Name}");
                    if(plugin.friendly_warn)
                    {
                        ev.Attacker.PersonalClearBroadcasts();
                        ev.Attacker.PersonalBroadcast(3, $"<color=#ff0000><size=25>《誤射に注意。味方へのダメージは容認されません。(<{ev.Player.Name}>への攻撃。)》\n</size><size=20>《Check your fire! Damage to ally forces not be tolerated.(Damaged to <{ev.Player.Name}>)》\n</size></color>", false);
                    }
                    if(plugin.friendly_warn_console)
                    {
                        ev.Attacker.SendConsoleMessage($"FriendlyFire to {ev.Player.Name}({ev.DamageType}:{(int)Math.Truncate(ev.Damage)})", "magenta");
                        ev.Player.SendConsoleMessage($"FriendlyFire from {ev.Attacker.Name}({ev.DamageType}:{(int)Math.Truncate(ev.Damage)})", "yellow");
                    }
                }
            }

            if(ev.Player.TeamRole.Role == Role.SCP_096)
            {
                if(Scp096PlayerScript.instance != null)
                {
                    Scp096PlayerScript.instance.IncreaseRage(1f);
                }
            }

            if(ev.DamageType == DamageType.SCP_106)
            {
                Scp079Interactable.ZoneAndRoom room = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp079PlayerScript>().GetOtherRoom();
                plugin.Debug($"{room.currentZone}:{room.currentRoom}");
                foreach(Scp079PlayerScript scp079 in Scp079PlayerScript.instances)
                {
                    Scp079Interactable.InteractableType[] filter = new Scp079Interactable.InteractableType[]
                    {
                            Scp079Interactable.InteractableType.Door,
                            Scp079Interactable.InteractableType.Light,
                            Scp079Interactable.InteractableType.Lockdown,
                            Scp079Interactable.InteractableType.Tesla,
                            Scp079Interactable.InteractableType.ElevatorUse
                    };
                    foreach(Scp079Interaction scp079int in scp079.ReturnRecentHistory(12f, filter))
                    {
                        foreach(Scp079Interactable.ZoneAndRoom hisroom in scp079int.interactable.currentZonesAndRooms)
                        {
                            if(hisroom.currentZone == room.currentZone && hisroom.currentRoom == room.currentRoom)
                            {
                                scp079.CallRpcGainExp(ExpGainType.KillAssist, (ev.Player.GetGameObject() as GameObject).GetComponent<CharacterClassManager>().curClass);
                            }
                        }
                    }
                }
            }

            if(ev.DamageType == DamageType.SCP_939 && dot_target.Find(x => x.PlayerId == ev.Player.PlayerId) == null && ev.Player.GetHealth() - ev.Damage >= 0)
            {
                dot_target.Add(ev.Player);
                Timing.RunCoroutine(SanyaPlugin._DOTDamage(ev.Player, 2, 1, 150 - (int)ev.Damage, DamageType.SCP_939), Segment.Update);
            }



            //ダメージ計算開始
            if(ev.DamageType == DamageType.USP)
            {
                if(ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                {
                    ev.Damage *= plugin.usp_damage_multiplier_scp;
                }
                else
                {
                    ev.Damage *= plugin.usp_damage_multiplier_human;
                }
            }
            else if(ev.DamageType == DamageType.FALLDOWN)
            {
                if(ev.Damage <= plugin.fallen_limit)
                {
                    ev.Damage = 0;
                }
            }
            else if(ev.DamageType == DamageType.FRAG && ev.Player.TeamRole.Role == Role.SCP_106)
            {
                ev.Damage /= 5;
            }

            //ロール除算計算開始
            if(ev.DamageType != DamageType.NUKE && ev.DamageType != DamageType.DECONT && ev.DamageType != DamageType.TESLA) //核と下層ロックとテスラ/HIDは対象外
            {
                if(ev.Player.TeamRole.Role == Role.SCP_173)
                {
                    ev.Damage /= plugin.damage_divisor_scp173;
                }
                else if(ev.Player.TeamRole.Role == Role.SCP_106)
                {
                    ev.Damage /= plugin.damage_divisor_scp106;
                }
                else if(ev.Player.TeamRole.Role == Role.SCP_049)
                {
                    ev.Damage /= plugin.damage_divisor_scp049;
                }
                else if(ev.Player.TeamRole.Role == Role.SCP_049_2)
                {
                    ev.Damage /= plugin.damage_divisor_scp049_2;
                }
                else if(ev.Player.TeamRole.Role == Role.SCP_096)
                {
                    ev.Damage /= plugin.damage_divisor_scp096;
                }
                else if(ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                {
                    ev.Damage /= plugin.damage_divisor_scp939;
                }
            }
            plugin.Debug($"[OnPlayerHurt] After {ev.Attacker.Name}<{ev.Attacker.TeamRole.Role}>({ev.DamageType}:{ev.Damage}) -> {ev.Player.Name}<{ev.Player.TeamRole.Role}>");
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            plugin.Debug($"[OnPlayerDie] {ev.Killer}:{ev.DamageTypeVar} -> {ev.Player.Name}");

            //scplist
            if(plugin.scp_disconnect_at_resetrole)
            {
                var target = scplist.Find(x => x.name == ev.Player.Name);
                if(target != null && ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                {
                    plugin.Warn($"[SCPList/Remove] {target.name}:{target.role}");
                    scplist.Remove(target);
                }
            }
            //----------------放送停止--------------
            if(plugin.Server.Map.GetIntercomSpeaker() != null)
            {
                if(ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                {
                    plugin.Server.Map.SetIntercomSpeaker(null);
                }
            }

            //----------------------------------------------------キル回復------------------------------------------------  
            //############## SCP-173 ###############
            if(!scp173_boosting &&                          //boostコマンド
                ev.DamageTypeVar == DamageType.SCP_173 &&
                ev.Killer.TeamRole.Role == Role.SCP_173 &&
                plugin.recovery_amount_scp173 > 0)
            {
                ev.Killer.SetHealth(Mathf.Clamp(ev.Killer.GetHealth() + plugin.recovery_amount_scp173, 0, ev.Killer.TeamRole.MaxHP), DamageType.NONE);
            }

            //############## SCP-096 ###############
            if(ev.DamageTypeVar == DamageType.SCP_096 &&
                ev.Killer.TeamRole.Role == Role.SCP_096 &&
                plugin.recovery_amount_scp096 > 0)
            {
                ev.Killer.SetHealth(Mathf.Clamp(ev.Killer.GetHealth() + plugin.recovery_amount_scp096, 0, ev.Killer.TeamRole.MaxHP), DamageType.NONE);
            }

            //############## SCP-939 ###############
            if(ev.DamageTypeVar == DamageType.SCP_939 &&
                (ev.Killer.TeamRole.Role == Role.SCP_939_53) || (ev.Killer.TeamRole.Role == Role.SCP_939_89) &&
                plugin.recovery_amount_scp939 > 0)
            {
                ev.Killer.SetHealth(Mathf.Clamp(ev.Killer.GetHealth() + plugin.recovery_amount_scp939, 0, ev.Killer.TeamRole.MaxHP), DamageType.NONE);
            }

            //############## SCP-049-2 ###############
            if(ev.DamageTypeVar == DamageType.SCP_049_2 &&
                ev.Killer.TeamRole.Role == Role.SCP_049_2 &&
                plugin.recovery_amount_scp049_2 > 0)
            {
                ev.Killer.SetHealth(Mathf.Clamp(ev.Killer.GetHealth() + plugin.recovery_amount_scp049_2, 0, ev.Killer.TeamRole.MaxHP), DamageType.NONE);
            }
            //----------------------------------------------------キル回復------------------------------------------------

            //----------------------------------------------------ペスト治療------------------------------------------------
            if(ev.DamageTypeVar == DamageType.SCP_049_2 && plugin.infect_by_scp049_2)
            {
                plugin.Info($"[Infector] 049-2 Infected!(Limit:{plugin.infect_limit_time}s) [{ev.Killer.Name}->{ev.Player.Name}]");
                ev.DamageTypeVar = DamageType.SCP_049;
                ev.Player.Infect(plugin.infect_limit_time);
            }
            //----------------------------------------------------ペスト治療------------------------------------------------
        }

        public void OnGrenadeHitPlayer(PlayerGrenadeHitPlayer ev)
        {
            plugin.Debug($"[OnGrenadeHitPlayer] {ev.Player?.Name}:{ev.Victim?.Name}");

            if(ev.Player != null && ev.Victim != null && ev.Player.PlayerId != ev.Victim.PlayerId)
            {
                SanyaPlugin.ShowHitmarker(ev.Player);
            }
        }

        public void OnLure(PlayerLureEvent ev)
        {
            if(plugin.scp106_lure_speaktime > 0)
            {
                if(plugin.Server.Map.GetIntercomSpeaker() != null)
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
            plugin.Debug($"[OnPocketDie] {ev.Player.Name}:{ev.Die}");

            //------------------------------ディメンション死亡回復(SCP-106)-----------------------------------------
            if(plugin.recovery_amount_scp106 > 0)
            {
                foreach(Player ply in plugin.Server.GetPlayers(Role.SCP_106))
                {
                    ply.SetHealth(Mathf.Clamp(ply.GetHealth() + plugin.recovery_amount_scp106, 0, ply.TeamRole.MaxHP), DamageType.NONE);
                }
            }

            foreach(Player ply in plugin.Server.GetPlayers(Role.SCP_106))
            {
                SanyaPlugin.ShowHitmarker(ply);
            }
        }

        public void OnRecallZombie(PlayerRecallZombieEvent ev)
        {
            plugin.Debug($"[OnRecallZombie] {ev.Player.Name} -> {ev.Target.Name} ({ev.AllowRecall})");

            if(ev.AllowRecall)
            {
                //----------------------治療回復SCP-049---------------------------------
                if(ev.Player.TeamRole.Role == Role.SCP_049 &&
                    plugin.recovery_amount_scp049 > 0)
                {
                    ev.Player.SetHealth(Mathf.Clamp(ev.Player.GetHealth() + plugin.recovery_amount_scp049, 0, ev.Player.TeamRole.MaxHP), DamageType.NONE);
                }

                (ev.Target.GetGameObject() as GameObject).GetComponent<CharacterClassManager>().NetworkdeathPosition = new Vector3(ev.Player.GetPosition().x, ev.Player.GetPosition().y, ev.Player.GetPosition().z);
            }
            else
            {
                ev.Player.PersonalClearBroadcasts();
                if(plugin.Server.GetPlayers().FindIndex(x => x.Name == ev.Target.Name) == -1)
                {
                    ev.Player.PersonalBroadcast(3, "<size=25>《治療失敗。対象プレイヤーは切断済みです。》\n </size><size=20>《Recall failed.》\n</size>", false);
                }
                else
                {
                    ev.Player.PersonalBroadcast(3, "<size=25>《治療失敗。対象プレイヤーはリスポーン済みです。》\n </size><size=20>《Recall failed.》\n</size>", false);
                }
            }
        }

        public void OnDoorAccess(PlayerDoorAccessEvent ev)
        {
            plugin.Debug($"[OnDoorAccess] {ev.Player.Name}:{ev.Door.Name}({ev.Door.Open}):{ev.Door.Permission}={ev.Allow}");

            //plugin.Debug($"name:{(ev.Door.GetComponent() as Door).name} parent:{(ev.Door.GetComponent() as Door).transform.parent.name} par-parent:{(ev.Door.GetComponent() as Door).transform.parent.parent.name} type:{(ev.Door.GetComponent() as Door).doorType}");

            if(plugin.inventory_card_act)
            {
                if(ev.Player.TeamRole.Team != Smod2.API.Team.SCP && !ev.Player.GetBypassMode() && !ev.Door.Locked)
                {
                    List<string> permlist = new List<string>();
                    foreach(Smod2.API.Item i in ev.Player.GetInventory())
                    {
                        foreach(Item item in itemtable)
                        {
                            if(item.id == (int)i.ItemType)
                            {
                                foreach(string p in item.permissions)
                                {
                                    if(permlist.IndexOf(p) == -1)
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

            switch(eventmode)
            {
                case SANYA_GAME_MODE.STORY:
                case SANYA_GAME_MODE.HCZ:
                    if(ev.Player.GetBypassMode())
                    {
                        break;
                    }
                    else if(ev.Player.TeamRole.Team != Smod2.API.Team.SCP)
                    {
                        foreach(Smod2.API.Item i in ev.Player.GetInventory())
                        {
                            if(i.ItemType == ItemType.SCIENTIST_KEYCARD && ev.Door.Name.StartsWith("CHECKPOINT_LCZ"))
                            {
                                plugin.Debug("[Eventer]HCZ-ScientistCard-Allow");
                                ev.Allow = true;
                            }
                        }
                    }
                    else if(ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                    {
                        if(startwait > plugin.Round.Duration)
                        {
                            plugin.Debug($"[Eventer]Too fast SCP : {startwait} > {plugin.Round.Duration}");
                            ev.Allow = false;
                        }
                    }
                    break;
            }

            if(plugin.handcuffed_cantopen)
            {
                if(ev.Player.IsHandcuffed())
                {
                    ev.Allow = false;
                }
            }
        }

        public void OnPlayerRadioSwitch(PlayerRadioSwitchEvent ev)
        {
            plugin.Debug($"[OnPlayerRadioSwitch] {ev.Player.Name}:{ev.ChangeTo}");

            if(plugin.radio_enhance)
            {
                if(ev.ChangeTo == RadioStatus.ULTRA_RANGE)
                {
                    Inventory inv = (ev.Player.GetGameObject() as GameObject).GetComponent<Inventory>();

                    if(plugin.Server.Map.GetIntercomSpeaker() == null && inv.items[ev.Player.GetItemIndex(ItemType.RADIO)].durability > 0)
                    {
                        plugin.Server.Map.SetIntercomSpeaker(ev.Player);
                    }
                }
                else
                {
                    if(plugin.Server.Map.GetIntercomSpeaker() != null)
                    {
                        if(ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
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

            if(eventmode == SANYA_GAME_MODE.NO_SCP)
            {
                if(ev.Elevator.ElevatorStatus == ElevatorStatus.Up && (ev.Elevator.ElevatorType == ElevatorType.GateA || ev.Elevator.ElevatorType == ElevatorType.GateB))
                {
                    Timing.RunCoroutine(SanyaPlugin._EscapeEmulateForElevator(ev.Elevator), Segment.Update);
                }
                else if(ev.Elevator.ElevatorStatus == ElevatorStatus.Down && (ev.Elevator.ElevatorType == ElevatorType.GateA || ev.Elevator.ElevatorType == ElevatorType.GateB))
                {
                    ev.AllowUse = false;
                }
            }

            if(plugin.handcuffed_cantopen)
            {
                if(ev.Player.IsHandcuffed())
                {
                    ev.AllowUse = false;
                }
            }
        }

        public void OnTeamRespawn(TeamRespawnEvent ev)
        {
            plugin.Debug($"[OnTeamRespawn] {ev.PlayerList.Count}:{ev.SpawnChaos}");

            if((plugin.stop_mtf_after_nuke && plugin.Server.Map.WarheadDetonated) || !roundduring)
            {
                ev.PlayerList.Clear();
            }

            if(eventmode == SANYA_GAME_MODE.NO_SCP)
            {
                plugin.Info($"[OnTeamRespawn] Elevator Spawn:{ev.PlayerList.Count}(isCI:{ev.SpawnChaos})");

                Vector pos = SanyaPlugin.GetSpawnElevatorPos(plugin.Server.Map.GetElevators().ToArray(), ev.SpawnChaos, true);
                Elevator elevator = null;
                foreach(var i in plugin.Server.Map.GetElevators())
                {
                    if(i.ElevatorType == ElevatorType.GateA && ev.SpawnChaos)
                    {
                        elevator = i;
                    }
                    else if(i.ElevatorType == ElevatorType.GateB && !ev.SpawnChaos)
                    {
                        elevator = i;
                    }
                }

                if(pos != null && elevator != null)
                {
                    Timing.RunCoroutine(SanyaPlugin._BeforeSpawnMoving(elevator), Segment.Update);
                    foreach(Player p in ev.PlayerList)
                    {
                        Vector rndpos = new Vector(pos.x + rnd.Next(-2, 2), pos.y, pos.z + rnd.Next(-2, 2));
                        Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(p, rndpos, false), Segment.Update);
                    }
                }
            }
        }

        public void On106CreatePortal(Player106CreatePortalEvent ev)
        {
            plugin.Debug($"[On106CreatePortal] {ev.Player.Name} ({ev.Position.x}/{ev.Position.y}/{ev.Position.z})");
        }

        public void On106Teleport(Player106TeleportEvent ev)
        {
            plugin.Debug($"[On106Teleport] {ev.Player.Name} ({ev.Position.x}/{ev.Position.y}/{ev.Position.z})");

            //Portal補正
            Scp106PlayerScript p106 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>();
            Vector3 pos = p106.portalPosition + Vector3.up * 1.5f;
            ev.Position = new Vector(pos.x, pos.y, pos.z);
        }

        public void OnSCP914Activate(SCP914ActivateEvent ev)
        {
            bool isAfterChange = false;
            Vector newpos = new Vector(ev.OutputPos.x, ev.OutputPos.y + 1.0f, ev.OutputPos.z);

            plugin.Debug($"[OnSCP914Activate] {ev.User.Name}/{ev.KnobSetting}");

            if(plugin.scp914_changing)
            {

                foreach(UnityEngine.Collider item in ev.Inputs)
                {
                    Pickup comp1 = item.GetComponent<Pickup>();
                    if(comp1 == null)
                    {
                        NicknameSync comp2 = item.GetComponent<NicknameSync>();
                        CharacterClassManager comp3 = item.GetComponent<CharacterClassManager>();
                        PlyMovementSync comp4 = item.GetComponent<PlyMovementSync>();
                        PlayerStats comp5 = item.GetComponent<PlayerStats>();

                        if(comp2 != null && comp3 != null && comp4 != null && comp5 != null && item.gameObject != null)
                        {
                            UnityEngine.GameObject gameObject = item.gameObject;
                            ServerMod2.API.SmodPlayer player = new ServerMod2.API.SmodPlayer(gameObject);

                            if(ev.KnobSetting == KnobSetting.COARSE)
                            {
                                plugin.Info($"[SCP914] COARSE:{player.Name}");

                                foreach(Smod2.API.Item hasitem in player.GetInventory())
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
                            else if(ev.KnobSetting == KnobSetting.ONE_TO_ONE && !isAfterChange)
                            {
                                plugin.Info($"[SCP914] 1to1:{player.Name}");

                                List<Player> speclist = new List<Player>();
                                foreach(Player spec in plugin.Server.GetPlayers(Role.SPECTATOR))
                                {
                                    if(spec.OverwatchMode == false)
                                    {
                                        speclist.Add(spec);
                                    }
                                }

                                plugin.Debug(speclist.Count.ToString());

                                if(speclist.Count > 0)
                                {
                                    int targetspec = rnd.Next(0, speclist.Count);

                                    speclist[targetspec].ChangeRole(player.TeamRole.Role, true, false, true, true);
                                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(speclist[targetspec], newpos, false));

                                    foreach(Smod2.API.Item specitem in speclist[targetspec].GetInventory())
                                    {
                                        specitem.Remove();
                                    }
                                    foreach(Smod2.API.Item hasitem in player.GetInventory())
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
                            else if(ev.KnobSetting == KnobSetting.FINE)
                            {
                                plugin.Info($"[SCP914] FINE:{player.Name}");

                                foreach(Smod2.API.Item hasitem in player.GetInventory())
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

                                plugin.Server.Map.SpawnItem((ItemType)rnd.Next(0, 31), newpos, new Vector(0, 0, 0));
                            }
                            else if(ev.KnobSetting == KnobSetting.VERY_FINE)
                            {
                                plugin.Info($"[SCP914] VERY_FINE:{player.Name}");

                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(0, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(ItemType.FLASHBANG, true, new Vector(-0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ChangeRole(Role.SCP_049_2, true, false, true, false);

                                Timing.RunCoroutine(SanyaPlugin._DOTDamage(player, 10, 0.5f, 99999, DamageType.DECONT), Segment.Update);
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

            if((plugin.stop_mtf_after_nuke && plugin.Server.Map.WarheadDetonated) || !roundduring)
            {
                //ev.AllowSummon = false;
            }
        }

        public void OnIntercom(PlayerIntercomEvent ev)
        {
            plugin.Debug($"[OnIntercom] {ev.Player.Name} : {ev.SpeechTime} : {ev.CooldownTime}");

            //Timing.RunCoroutine(SanyaPlugin.DeductBatteryHasTransmission(ev.Player), Segment.Update);
        }

        public void OnGeneratorAccess(PlayerGeneratorAccessEvent ev)
        {
            plugin.Debug($"[OnGeneratorAccess] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}:{ev.Generator.Engaged}");

            if(plugin.generator_engaged_cantopen)
            {
                if(ev.Generator.Engaged)
                {
                    ev.Allow = false;
                }
            }
        }

        public void OnGeneratorUnlock(PlayerGeneratorUnlockEvent ev)
        {
            plugin.Debug($"[OnGeneratorUnlock] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}:{ev.Generator.Engaged}");
            foreach(string p in genperm)
            {
                plugin.Debug($"genperm:{p}");
            }

            if(plugin.inventory_card_act)
            {
                if(ev.Player.TeamRole.Team != Smod2.API.Team.SCP && !ev.Generator.Engaged && !ev.Player.GetBypassMode())
                {
                    List<string> permlist = new List<string>();
                    foreach(Smod2.API.Item i in ev.Player.GetInventory())
                    {
                        foreach(Item item in itemtable)
                        {
                            if(item.id == (int)i.ItemType)
                            {
                                foreach(string p in item.permissions)
                                {
                                    if(permlist.IndexOf(p) == -1)
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

            if(ev.Allow)
            {
                ev.Generator.Open = true;
            }
        }

        public void OnGeneratorInsertTablet(PlayerGeneratorInsertTabletEvent ev)
        {
            plugin.Debug($"[OnGeneratorInsertTablet] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}({ev.RemoveTablet})");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}:{ev.Generator.Engaged}");

            if(plugin.cassie_subtitle)
            {
                if(ev.Allow)
                {
                    string[] genname = SanyaPlugin.TranslateGeneratorName(ev.Generator.Room.RoomType);

                    if(roundduring)
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
            foreach(Generator gens in plugin.Server.Map.GetGenerators())
            {
                if(gens.Engaged && ev.Generator.Room.RoomType != gens.Room.RoomType)
                {
                    engcount++;
                }
            }

            if(engcount >= 5)
            {
                gencomplete = true;
            }

            if(plugin.generator_engaged_cantopen)
            {
                ev.Generator.Open = false;
            }

            if(plugin.cassie_subtitle)
            {
                string[] genname = SanyaPlugin.TranslateGeneratorName(ev.Generator.Room.RoomType);

                if(roundduring)
                {
                    if(!gencomplete)
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

            if(ev.Allow && !(ev.Door.GetComponent() as Door).moving.moving)
            {
                SanyaPlugin.CallAmbientSound(rnd.Next(0, 32));
                if(ev.Door.Locked)
                {
                    if(ev.Door.Open)
                    {
                        ev.Door.Open = false;
                    }
                    else
                    {
                        ev.Door.Open = true;
                    }
                }
            }
        }

        public void On079Elevator(Player079ElevatorEvent ev)
        {
            plugin.Debug($"[On079Elevator] {ev.Player.Name} (Tier:{ev.Player.Scp079Data.Level + 1}:{ev.Elevator.ElevatorType}):{ev.Allow}");

            if(ev.Allow && ev.Elevator.ElevatorStatus != ElevatorStatus.Moving)
            {
                SanyaPlugin.CallAmbientSound(rnd.Next(0, 32));
            }
        }

        public void On079Lockdown(Player079LockdownEvent ev)
        {
            plugin.Debug($"[On079Lockdown] {ev.Player.Name}(Tier{ev.Player.Scp079Data.Level + 1}:{ev.Room.RoomType}({ev.APDrain}):{ev.Allow}");

            if(ev.Allow)
            {
                SanyaPlugin.CallAmbientSound(rnd.Next(0, 32));
            }

            if(ev.Room.RoomType == RoomType.SCP_914)
            {
                SanyaPlugin.Call914Use(ev.Player);
            }

            if(ev.Player.Scp079Data.Level >= 2)
            {
                plugin.Debug("079LockDown-HCZBlackout");
                Generator079.mainGenerator.CallRpcOvercharge();
                if(!plugin.Server.Map.LCZDecontaminated)
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

            foreach(int item in plist)
            {
                if(playeridlist[item] > 0)
                {
                    playeridlist[item] -= 1;
                }
            }

            if(updatecounter % 60 == 0 && running && roundduring)
            {
                updatecounter = 0;

                if(scp079_boost_cooltime > 0)
                {
                    scp079_boost_cooltime--;
                }

                if(scp079_attack_cooltime > 0)
                {
                    scp079_attack_cooltime--;
                }

                if(eventmode == SANYA_GAME_MODE.STORY || eventmode == SANYA_GAME_MODE.HCZ)
                {
                    if(startwait < plugin.Round.Duration && !storyshaker)
                    {
                        plugin.Server.Map.Shake();
                        storyshaker = true;
                    }
                }

                if(eventmode == SANYA_GAME_MODE.NIGHT)
                {
                    if(!gencomplete)
                    {
                        if(flickcount >= 10)
                        {
                            plugin.Debug("HCZ Flick");
                            Generator079.mainGenerator.CallRpcOvercharge();
                            flickcount = 0;
                        }
                        else
                        {
                            flickcount++;
                        }

                        if(flickcount_lcz >= 8)
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

                if(plugin.original_auto_nuke && eventmode != SANYA_GAME_MODE.NO_SCP)
                {
                    if(!AlphaWarheadController.host.inProgress)
                    {
                        if(plugin.Round.Stats.ScientistsAlive < 1 && !isScientistAllDead && plugin.Server.Map.LCZDecontaminated)
                        {
                            plugin.Info($"[AutoNuke] Sector 1 (Duration:{plugin.Round.Duration})");
                            plugin.Info($"Class-D:{plugin.Round.Stats.ClassDAlive} Scientist:{plugin.Round.Stats.ScientistsAlive} NTF:{plugin.Round.Stats.NTFAlive} SCP:{plugin.Round.Stats.SCPAlive} CI:{plugin.Round.Stats.CiAlive}");

                            isScientistAllDead = true;
                            AlphaWarheadController.host.InstantPrepare();
                            AlphaWarheadController.host.StartDetonation();
                        }
                        else
                        {
                            if(plugin.Round.Stats.ClassDAlive < 1 && plugin.Round.Stats.NTFAlive < 5 && !isClassDAllDead && isScientistAllDead
                                || (plugin.Round.Duration >= plugin.original_auto_nuke_force_sector2 && plugin.original_auto_nuke_force_sector2 > 0))
                            {
                                plugin.Info($"[AutoNuke] Sector 2 (Duration:{plugin.Round.Duration})");
                                plugin.Info($"Class-D:{plugin.Round.Stats.ClassDAlive} Scientist:{plugin.Round.Stats.ScientistsAlive} NTF:{plugin.Round.Stats.NTFAlive} SCP:{plugin.Round.Stats.SCPAlive} CI:{plugin.Round.Stats.CiAlive}");

                                isLocked = true;
                                isClassDAllDead = true;

                                foreach(Smod2.API.Door item in plugin.Server.Map.GetDoors())
                                {
                                    if(item.Name.Contains("GATE_") || item.Name.Contains("CHECKPOINT_"))
                                    {
                                        item.Open = true;
                                        item.Locked = true;
                                    }
                                }

                                AlphaWarheadController.host.Networksync_resumeScenario = 4;
                                AlphaWarheadController.host.InstantPrepare();
                                AlphaWarheadController.host.StartDetonation();
                                AlphaWarheadController.host.SetLocked(true);
                            }
                        }
                    }
                }

                if(plugin.summary_less_mode)
                {
                    if(isEnded)
                    {
                        roundduring = false;

                        int restarttime = ConfigManager.Manager.Config.GetIntValue("auto_round_restart_time", 10);

                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast((uint)restarttime, $"<size=35>《ラウンドが終了しました。{restarttime}秒後にリスタートします。》\n </size><size=25>《Round Ended. Restart after {restarttime} seconds.》\n</size>", false);
                        EventManager.Manager.HandleEvent<IEventHandlerRoundEnd>(new RoundEndEvent(plugin.Server, plugin.Round, ROUND_END_STATUS.FORCE_END));
                        Timing.RunCoroutine(SanyaPlugin._SummaryLessRoundrestart(plugin, restarttime), Segment.Update);
                    }
                }

                if(plugin.intercom_information)
                {
                    GameObject host = UnityEngine.GameObject.Find("Host");
                    bool nextIsCI = host.GetComponent<MTFRespawn>().nextWaveIsCI;
                    string nextRespawnName = "";
                    int nextRespawn = (int)Math.Truncate(host.GetComponent<MTFRespawn>().timeToNextRespawn);
                    int leftAutoWarhead = (int)Math.Truncate(AlphaWarheadController.host.smtimeToScheduledDetonation);
                    int leftDetonation = 0;
                    int leftDecontamination = (int)((Math.Truncate(((11.74f * 60) * 100f)) / 100f) - (Math.Truncate(host.GetComponent<DecontaminationLCZ>().time * 100f) / 100f));

                    if(nextIsCI)
                    {
                        nextRespawnName = "CI";
                    }
                    else
                    {
                        nextRespawnName = "MTF";
                    }

                    if(plugin.original_auto_nuke && plugin.original_auto_nuke_force_sector2 > 0)
                    {
                        leftAutoWarhead = Mathf.Clamp(plugin.original_auto_nuke_force_sector2 - plugin.Round.Duration, 0, plugin.original_auto_nuke_force_sector2);
                    }

                    if(AlphaWarheadController.host.inProgress)
                    {
                        leftDetonation = (int)Math.Truncate(AlphaWarheadController.host.timeToDetonation);
                    }
                    else
                    {
                        if(AlphaWarheadController.host.sync_resumeScenario >= 0)
                        {
                            leftDetonation = AlphaWarheadController.host.scenarios_resume[AlphaWarheadController.host.sync_resumeScenario].tMinusTime + 6;
                        }
                        else
                        {
                            leftDetonation = AlphaWarheadController.host.scenarios_start[AlphaWarheadController.host.sync_startScenario].tMinusTime + 11;
                        }

                    }

                    string content = string.Concat(
                            $"MISSION START : {SanyaPlugin.roundStartTime.ToString("yyyy/MM/dd HH:mm:ss")}\n",
                            $"MISSION TIMER : {(plugin.Server.Round.Duration / 60).ToString("00")}:{(plugin.Server.Round.Duration % 60).ToString("00")}\n",
                            $"SCP LEFT : {(plugin.Server.Round.Stats.SCPAlive).ToString("00")}/{(plugin.Server.Round.Stats.SCPStart).ToString("00")}\n",
                            $"CLASS-D LEFT : {(plugin.Server.Round.Stats.ClassDAlive).ToString("00")}/{(plugin.Server.Round.Stats.ClassDStart).ToString("00")}\n",
                            $"SCIENTIST LEFT : {(plugin.Server.Round.Stats.ScientistsAlive).ToString("00")}/{(plugin.Server.Round.Stats.ScientistsStart).ToString("00")}\n",
                            $"DECONT LEFT : {(leftDecontamination / 60).ToString("00")}:{(leftDecontamination % 60).ToString("00")}\n",
                            $"DETONATE LEFT : {(leftDetonation / 60).ToString("00")}:{(leftDetonation % 60).ToString("00")}\n",
                            $"AUTO WARHEAD LEFT : {(leftAutoWarhead / 60).ToString("00")}:{(leftAutoWarhead % 60).ToString("00")}\n",
                            $"NEXT {nextRespawnName} ENTER : {(nextRespawn / 60).ToString("00")}:{(nextRespawn % 60).ToString("00")}\n"
                    );

                    plugin.Server.Map.SetIntercomContent(IntercomStatus.Ready,
                        string.Concat(
                            content,
                            $"\nREADY"
                        )
                    );
                    plugin.Server.Map.SetIntercomContent(IntercomStatus.Transmitting,
                        string.Concat(
                            content,
                            $"\nTRANSMITTING : "
                        )
                    );
                    plugin.Server.Map.SetIntercomContent(IntercomStatus.Restarting,
                        string.Concat(
                            content,
                            $"\nRESTARTING : "
                        )
                    );
                    plugin.Server.Map.SetIntercomContent(IntercomStatus.Muted,
                        string.Concat(
                            content,
                            $"\nACCESS DENIED"
                        )
                    );
                }

                if(plugin.title_timer)
                {
                    string title = ConfigManager.Manager.Config.GetStringValue("player_list_title", "Unnamed Server") + " RoundTime: " + plugin.Server.Round.Duration / 60 + ":" + plugin.Server.Round.Duration % 60;
                    plugin.Server.PlayerListTitle = title;
                }

                if(plugin.traitor_limitter > 0)
                {
                    try
                    {
                        foreach(Player ply in plugin.Server.GetPlayers())
                        {
                            if((ply.TeamRole.Role == Role.NTF_CADET ||
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

                                if((pos.x >= 172 && pos.x <= 182) &&
                                    (pos.y >= 980 && pos.y <= 990) &&
                                    (pos.z >= 25 && pos.z <= 34))
                                {
                                    if((ply.TeamRole.Role == Role.CHAOS_INSURGENCY && cicount <= plugin.traitor_limitter) ||
                                        (ply.TeamRole.Role != Role.CHAOS_INSURGENCY && ntfcount <= plugin.traitor_limitter))
                                    {
                                        int rndresult = rnd.Next(0, 100);
                                        plugin.Debug($"[TraitorCheck] Traitoring... [{ply.Name}:{ply.TeamRole.Role}:{rndresult}<={plugin.traitor_chance_percent}]");
                                        if(rndresult <= plugin.traitor_chance_percent)
                                        {
                                            if(ply.TeamRole.Role == Role.CHAOS_INSURGENCY)
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
                                            ply.Kill(DamageType.RAGDOLLLESS);
                                            plugin.Info($"[TraitorCheck] Failed [{ply.Name}:{ply.TeamRole.Role}:{rndresult}>={plugin.traitor_chance_percent}]");
                                        }
                                    };
                                }
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        plugin.Error(e.Message);
                    }
                }

                if(!Timing.IsRunning(infosender))
                {
                    infosender = Timing.RunCoroutine(this.Sender(), Segment.Update, "InfoSender");
                }
            }
            updatecounter++;
        }

        public void OnCallCommand(PlayerCallCommandEvent ev)
        {
            plugin.Debug($"[OnCallCommand] [Before] Called:{ev.Player.Name} Command:{ev.Command} Return:{ev.ReturnMessage}");

            int cooltime = -1;
            if(playeridlist.TryGetValue(ev.Player.PlayerId, out cooltime))
            {
                if(cooltime == 0 || ev.Player.GetBypassMode())
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

            if(ev.ReturnMessage == "Command not found.")
            {
                if(ev.Command.StartsWith("help"))
                {
                    ev.ReturnMessage = "SanyaPlugin Command List\n" +
                        ".kill 自殺します。\n" +
                        ".sinfo SCP時のみ仲間のSCPリストを表示します。\n" +
                        ".939sp SCP-939で放送が可能です。Vキーの人間向けにて使用可能。\n" +
                        ".079nuke 核の起動/停止を行えます。(Tier3以上/AP125消費)\n" +
                        ".radio 無線機のオンオフが行えます。\n" +
                        ".boost SCPの各種ブーストが使用可能。\n" +
                        ".attack 079の場合カメラ射撃、人間の場合素手の時格闘が出来ます。";
                }
                else if(ev.Command.StartsWith("kill"))
                {
                    if(roundduring)
                    {
                        if(ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR && ev.Player.TeamRole.Team != Smod2.API.Team.NONE && ev.Player.TeamRole.Team != Smod2.API.Team.SCP)
                        {
                            plugin.Debug($"[Suicide] {ev.Player.Name}");
                            if(!plugin.suicide_need_weapon)
                            {
                                ev.ReturnMessage = "Success.";
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(3, "<size=25>《あなたは自殺しました。》\n </size><size=20>《You suicided.》\n</size>", false);
                                ev.Player.SetGodmode(false);
                                ev.Player.Kill(DamageType.TESLA);
                            }
                            else
                            {
                                if(ev.Player.GetCurrentItemIndex() > -1)
                                {
                                    switch(ev.Player.GetCurrentItem().ItemType)
                                    {
                                        case ItemType.FRAG_GRENADE:
                                            ev.ReturnMessage = "Success.";
                                            ev.Player.PersonalClearBroadcasts();
                                            ev.Player.PersonalBroadcast(3, "<size=25>《あなたはグレネードのピンを抜き、自爆します。》\n </size><size=20>《You suicided.》\n</size>", false);
                                            ev.Player.SetGodmode(false);
                                            foreach(Smod2.API.Item i in ev.Player.GetInventory().FindAll(x => x.ItemType == ItemType.FRAG_GRENADE))
                                            {
                                                i.Remove();
                                            }
                                            Timing.RunCoroutine(SanyaPlugin._SuicideWithFollowingGrenade(ev.Player), Segment.Update);
                                            break;
                                        case ItemType.COM15:
                                        case ItemType.P90:
                                        case ItemType.USP:
                                            ev.ReturnMessage = "Success.";
                                            ev.Player.PersonalClearBroadcasts();
                                            ev.Player.PersonalBroadcast(3, "<size=25>《あなたは自殺しました。》\n </size><size=20>《You suicided.》\n</size>", false);
                                            ev.Player.SetGodmode(false);
                                            Timing.RunCoroutine(SanyaPlugin._DelayedSuicideWithWeaponSound(ev.Player, DamageType.COM15), Segment.Update);
                                            break;
                                        case ItemType.E11_STANDARD_RIFLE:
                                            ev.ReturnMessage = "Success.";
                                            ev.Player.PersonalClearBroadcasts();
                                            ev.Player.PersonalBroadcast(3, "<size=25>《あなたは自殺しました。》\n </size><size=20>《You suicided.》\n</size>", false);
                                            ev.Player.SetGodmode(false);
                                            Timing.RunCoroutine(SanyaPlugin._DelayedSuicideWithWeaponSound(ev.Player, DamageType.E11_STANDARD_RIFLE), Segment.Update);
                                            break;
                                        case ItemType.LOGICER:
                                        case ItemType.MP4:
                                            ev.ReturnMessage = "Success.";
                                            ev.Player.PersonalClearBroadcasts();
                                            ev.Player.PersonalBroadcast(3, "<size=25>《あなたは自殺しました。》\n </size><size=20>《You suicided.》\n</size>", false);
                                            ev.Player.SetGodmode(false);
                                            Timing.RunCoroutine(SanyaPlugin._DelayedSuicideWithWeaponSound(ev.Player, DamageType.LOGICER), Segment.Update);
                                            break;
                                        default:
                                            ev.ReturnMessage = "手に武器を持っていません。";
                                            break;
                                    }
                                }
                                else
                                {
                                    ev.ReturnMessage = "手に何も持っていません。";
                                }
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = "あなたは観戦かSCPです。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if(ev.Command.StartsWith("sinfo"))
                {
                    if(roundduring)
                    {
                        if(ev.Player.TeamRole.Team == Smod2.API.Team.SCP || ev.Player.GetBypassMode())
                        {
                            plugin.Debug($"[SCPInfo] {ev.Player.Name}");

                            List<Player> scps = plugin.Server.GetPlayers().FindAll(fl => { return fl.TeamRole.Team == Smod2.API.Team.SCP; });
                            int scp049_2_count = scps.FindAll(x => x.TeamRole.Role == Role.SCP_049_2).Count;
                            string scplist = $"仲間のSCP情報[計{scps.Count}体 / 049-2:{scp049_2_count}体]：\n";
                            foreach(Player items in scps)
                            {
                                if(items.TeamRole.Role == Role.SCP_049_2)
                                {
                                    continue;
                                }

                                if(items.TeamRole.Role == Role.SCP_079)
                                {
                                    Scp079PlayerScript ply079 = (items.Scp079Data.GetComponent() as Scp079PlayerScript);
                                    RaycastHit raycastHit;
                                    string zonename = "不明";
                                    if(Physics.Raycast(new Ray(ply079.currentCamera.transform.position, Vector3.down), out raycastHit, 100f, Interface079.singleton.roomDetectionMask))
                                    {
                                        Transform transform = raycastHit.transform;
                                        while(transform != null && !transform.transform.name.ToUpper().Contains("ROOT"))
                                        {
                                            transform = transform.transform.parent;
                                        }
                                        if(transform != null)
                                        {
                                            if(transform.transform.parent.name.Contains("Out"))
                                            {
                                                zonename = "地上";
                                            }
                                            else if(transform.transform.parent.name.Contains("Entrance"))
                                            {
                                                zonename = "上層";
                                            }
                                            else if(transform.transform.parent.name.Contains("Heavy"))
                                            {
                                                zonename = "中層";
                                            }
                                            else if(transform.transform.parent.name.Contains("Light"))
                                            {
                                                zonename = "下層";
                                            }
                                        }
                                    }
                                    scplist += $"{items.Name} : {items.TeamRole.Name}(Tier{items.Scp079Data.Level + 1}:{(int)Math.Truncate(items.Scp079Data.AP)}AP) : {zonename} : {ply079.currentCamera.cameraName}\n";
                                }
                                else
                                {
                                    GameObject gameObject = items.GetGameObject() as GameObject;
                                    Vector3 pos = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.8f, gameObject.transform.position.z);
                                    RaycastHit raycastHit;
                                    string zonename = "不明";
                                    if(Physics.Raycast(new Ray(pos, Vector3.down), out raycastHit, 100f, FallDamage.staticGroundMask))
                                    {
                                        if(raycastHit.transform.root.name.Contains("Out"))
                                        {
                                            zonename = "地上";
                                        }
                                        else if(raycastHit.transform.root.name.Contains("Entrance"))
                                        {
                                            zonename = "上層";
                                        }
                                        else if(raycastHit.transform.root.name.Contains("Heavy"))
                                        {
                                            zonename = "中層";
                                        }
                                        else if(raycastHit.transform.root.name.Contains("Light"))
                                        {
                                            zonename = "下層";
                                        }
                                    }
                                    scplist += $"{items.Name} : {items.TeamRole.Name}({items.GetHealth()}HP) : {zonename}\n";
                                }
                            }

                            ev.ReturnMessage = scplist;
                            ev.Player.PersonalClearBroadcasts();
                            ev.Player.PersonalBroadcast(5, $"<size=25>{scplist}</size>", false);
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
                else if(ev.Command.StartsWith("939sp"))
                {
                    if(roundduring)
                    {
                        if(ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                        {
                            plugin.Debug($"[939Speaker] {ev.Player.Name}");

                            if(null == plugin.Server.Map.GetIntercomSpeaker())
                            {
                                plugin.Server.Map.SetIntercomSpeaker(ev.Player);
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(3, "<size=25>《放送を開始します。》\n </size><size=20>《You will broadcast.》\n</size>", false);
                                ev.ReturnMessage = "放送を開始します。";
                            }
                            else
                            {
                                if(ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                                {
                                    plugin.Server.Map.SetIntercomSpeaker(null);
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, "<size=25>《放送を終了しました。》\n </size><size=20>《You finished broadcasting.》\n</size>", false);
                                    ev.ReturnMessage = "放送を終了しました。";
                                }
                                else
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, "<size=25>《誰かが放送中です。》\n </size><size=20>《Someone is broadcasting.》\n</size>", false);
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
                else if(ev.Command.StartsWith("079nuke"))
                {
                    if(roundduring)
                    {
                        if(ev.Player.TeamRole.Role == Role.SCP_079)
                        {
                            plugin.Info($"[079nuke] {ev.Player.Name} [Tier:{ev.Player.Scp079Data.Level + 1} AP:{ev.Player.Scp079Data.AP}]");
                            if(ev.Player.Scp079Data.Level >= 2 && ev.Player.Scp079Data.AP >= 125)
                            {
                                if(!AlphaWarheadController.host.inProgress)
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

                                if(!ev.Player.GetBypassMode())
                                {
                                    ev.Player.Scp079Data.AP -= 125.0f;
                                }
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
                else if(ev.Command.StartsWith("079sp"))
                {
                    if(roundduring)
                    {
                        if(ev.Player.TeamRole.Role == Role.SCP_079)
                        {
                            plugin.Info($"[079sp] {ev.Player.Name} [Tier:{ev.Player.Scp079Data.Level + 1} AP:{ev.Player.Scp079Data.AP}]");
                            if(plugin.Server.Map.GetIntercomSpeaker() != null)
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
                else if(ev.Command.StartsWith("radio"))
                {
                    if(roundduring)
                    {
                        Radio radio = (ev.Player.GetGameObject() as GameObject).GetComponent<Radio>();
                        if(radio != null
                            && ev.Player.TeamRole.Team != Smod2.API.Team.SCP
                            && ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR
                            )
                        {
                            if(ev.Player.GetInventory().FindIndex(x => x.ItemType == ItemType.RADIO) != -1)
                            {
                                if(ev.Player.RadioStatus != RadioStatus.CLOSE)
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, $"<size=25>《無線機をオフにしました。》\n </size><size=20>《Disabled radio.》\n</size>", false);
                                    radio.CallCmdUpdatePreset((int)RadioStatus.CLOSE);
                                }
                                else
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, $"<size=25>《無線機をオンにしました。》\n </size><size=20>《Enabled radio.》\n</size>", false);
                                    radio.CallCmdUpdatePreset((int)RadioStatus.SHORT_RANGE);
                                }
                                ev.ReturnMessage = $"無線機の状態:{ev.Player.RadioStatus}";
                            }
                            else
                            {
                                ev.ReturnMessage = "無線機を持っていません。";
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = "人間陣営ではありません。";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if(ev.Command.StartsWith("boost"))
                {
                    if(roundduring)
                    {
                        if(ev.Player.TeamRole.Role == Role.SCP_096)
                        {
                            Scp096PlayerScript ply096 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp096PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                            if(health > usehp && ply096 != null)
                            {
                                if(ply096.Networkenraged == Scp096PlayerScript.RageState.Enraged)
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, $"<size=25>《SCP-096ブーストを使用しました。(HP消費:{usehp})》\n </size><size=20>《Used <SCP-096> boost. (used HP:{usehp})》\n</size>", false);
                                    if(!ev.Player.GetBypassMode())
                                    {
                                        ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                    }
                                    ply096.rageProgress = 1f;
                                    ev.ReturnMessage = $"Boost!(-{usehp})";
                                }
                                else
                                {
                                    ev.ReturnMessage = "発狂中ではありません。";
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else if(ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                        {
                            Scp939PlayerScript ply939 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp939PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                            bool canboost = false;
                            if(ev.Player.TeamRole.Role == Role.SCP_939_53 && !scp939_53_boosting)
                            {
                                canboost = true;
                                scp939_53_boosting = true;
                            }
                            else if(ev.Player.TeamRole.Role == Role.SCP_939_89 && !scp939_89_boosting)
                            {
                                canboost = true;
                                scp939_89_boosting = true;
                            }

                            if(health > usehp && ply939 != null)
                            {
                                if(canboost)
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, $"<size=25>《SCP-939ブーストを使用しました。(HP消費:{usehp})》\n </size><size=20>《Used <SCP-939> boost. (used HP:{usehp})》\n</size>", false);
                                    if(!ev.Player.GetBypassMode())
                                    {
                                        ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                    }

                                    Timing.RunCoroutine(SanyaPlugin._939Boost(ev.Player), Segment.Update);

                                    ev.ReturnMessage = $"Boost!(-{usehp})";
                                }
                                else
                                {
                                    ev.ReturnMessage = "既に使用中です。";
                                }
                               
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else if(ev.Player.TeamRole.Role == Role.SCP_106)
                        {
                            if(!(ev.Player.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>().goingViaThePortal)
                            {

                                if((ev.Player.GetGameObject() as GameObject).GetComponent<FallDamage>().isGrounded)
                                {
                                    Scp106PlayerScript ply106 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>();
                                    int health = ev.Player.GetHealth();
                                    int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                                    List<Player> humanlist = new List<Player>();
                                    foreach(Player item in plugin.Server.GetPlayers())
                                    {
                                        if(item.TeamRole.Team != Smod2.API.Team.SCP &&
                                            item.TeamRole.Team != Smod2.API.Team.SPECTATOR &&
                                            item.TeamRole.Team != Smod2.API.Team.NONE &&
                                            (item.GetGameObject() as GameObject).GetComponent<FallDamage>().isGrounded)
                                        {
                                            if(!(item.GetPosition().y < -1900 && item.GetPosition().y > -2000))
                                            {
                                                humanlist.Add(item);
                                            }
                                        }
                                    }
                                    if(plugin.Server.Round.Duration > plugin.scp106_portal_to_human_wait)
                                    {
                                        if(health > usehp)
                                        {
                                            if(humanlist.Count > 0)
                                            {
                                                int rndres = rnd.Next(0, humanlist.Count);
                                                ev.Player.PersonalClearBroadcasts();
                                                ev.Player.PersonalBroadcast(3, $"<size=25>《生存者<{humanlist[rndres].Name}>の近くにワープします。》\n </size><size=20>《Warp to <{humanlist[rndres].Name}>.》\n</size>", false);
                                                ev.ReturnMessage = $"{humanlist[rndres].Name}の近くにワープ。(-{usehp})";
                                                if(!ev.Player.GetBypassMode())
                                                {
                                                    ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                                }
                                                SanyaPlugin.ShowHitmarker(ev.Player);
                                                Timing.RunCoroutine(SanyaPlugin._106CreatePortalEX(ev.Player, humanlist[rndres]), Segment.Update);
                                            }
                                            else
                                            {
                                                ev.Player.PersonalClearBroadcasts();
                                                ev.Player.PersonalBroadcast(3, $"<size=25>《ターゲットが見つからないようだ。》\n </size><size=20>《Target not found.》\n</size>", false);
                                                ev.ReturnMessage = "No target";
                                            }
                                        }
                                        else
                                        {
                                            ev.ReturnMessage = "HPが足りません。";
                                        }
                                    }
                                    else
                                    {
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(3, $"<size=25>《まだ使えないようだ。》\n </size><size=20>《Not available yet.》\n</size>", false);
                                        ev.ReturnMessage = "まだ使えません。";
                                    }
                                }
                                else
                                {
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, $"<size=25>《空中では使えない。》\n </size><size=20>《Don't jump for use.》\n</size>", false);
                                    ev.ReturnMessage = "空中では使えません。";
                                }
                            }
                            else
                            {
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(3, $"<size=25>《ワープ中は使えない。》\n </size><size=20>《Don't warping for use.》\n</size>", false);
                                ev.ReturnMessage = "ワープ中は使えません。";
                            }
                        }
                        else if(ev.Player.TeamRole.Role == Role.SCP_049)
                        {
                            Scp049PlayerScript ply049 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp049PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                            if(health > usehp && ply049 != null)
                            {
                                List<Ragdoll> rags = new List<Ragdoll>(GameObject.FindObjectsOfType<Ragdoll>());

                                plugin.Debug($"rags:{rags.Count}");

                                foreach(Ragdoll i in rags)
                                {
                                    plugin.Debug($"{i.owner.steamClientName}:{(Role)i.owner.charclass}");

                                    if( i.owner.charclass != (int)Role.SCP_049
                                        && i.owner.charclass != (int)Role.SCP_096
                                        && i.owner.charclass != (int)Role.SCP_106
                                        && i.owner.charclass != (int)Role.SCP_173
                                        && i.owner.charclass != (int)Role.SCP_939_53
                                        && i.owner.charclass != (int)Role.SCP_939_89
                                        )
                                    {
                                        i.NetworkallowRecall = true;
                                        i.owner.deathCause.tool = (int)DamageType.SCP_049;
                                        foreach(Smod2.API.Player p in plugin.Server.GetPlayers())
                                        {
                                            if(p.PlayerId == i.owner.PlayerId)
                                            {
                                                p.Infect(60f);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        plugin.Debug($"Bypass:{(Role)i.owner.charclass}");
                                    }
                                }

                                if(!ev.Player.GetBypassMode())
                                {
                                    ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                }
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(3, $"<size=25>《SCP-049ブーストを使用しました。(HP消費:{usehp})》\n </size><size=20>《Used <SCP-049> boost. (used HP:{usehp})》\n</size>", false);
                                ev.ReturnMessage = $"Boost!(-{usehp})";
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else if(ev.Player.TeamRole.Role == Role.SCP_173)
                        {
                            GameObject gameObject = (ev.Player.GetGameObject() as GameObject);
                            Scp173PlayerScript ply173 = gameObject.GetComponent<Scp173PlayerScript>();
                            Scp049PlayerScript ply049 = gameObject.GetComponent<Scp049PlayerScript>();
                            int health = ev.Player.GetHealth();
                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);

                            if(health > usehp && ply173 != null && ply049 != null)
                            {
                                foreach(Scp173PlayerScript scp173PlayerScript in GameObject.FindObjectsOfType<Scp173PlayerScript>())
                                {
                                    if(!scp173PlayerScript.iAm173)
                                    {
                                        scp173PlayerScript.CallRpcBlinkTime();
                                    }
                                }

                                if(!ev.Player.GetBypassMode())
                                {
                                    ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                }

                                RaycastHit raycastHit;
                                if(Physics.Raycast(ply049.plyCam.transform.position, ply049.plyCam.transform.forward, out raycastHit, 500f, ply173.teleportMask) && raycastHit.transform.GetComponent<CharacterClassManager>() != null)
                                {
                                    Vector3 b = raycastHit.transform.position - gameObject.transform.position;
                                    b = b.normalized * Mathf.Clamp(b.magnitude - 1f, 0f, 500f);
                                    gameObject.GetComponent<PlyMovementSync>().SetPosition(gameObject.transform.position + b);
                                    var target = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                    if(target.PlayerId != ev.Player.PlayerId)
                                    {
                                        SanyaPlugin.ShowHitmarker(ev.Player);
                                        SanyaPlugin.Call173SnapSound(target);
                                        gameObject.GetComponent<PlayerStats>().HurtPlayer(
                                            new PlayerStats.HitInfo(999990f,
                                            gameObject.GetComponent<NicknameSync>().myNick + " (" + gameObject.GetComponent<CharacterClassManager>().SteamId + ")",
                                            DamageTypes.Scp173,
                                            gameObject.GetComponent<QueryProcessor>().PlayerId),
                                            raycastHit.transform.gameObject);
                                    }
                                }
                                else
                                {
                                    RaycastHit raycastHit2;
                                    if(Physics.Raycast(ply049.plyCam.transform.position, ply049.plyCam.transform.forward, out raycastHit2, 500f, ply173.teleportMask))
                                    {
                                        Vector3 b = raycastHit2.point - gameObject.transform.position;
                                        b = b.normalized * Mathf.Clamp(b.magnitude - 1f, 0f, 20.3f);
                                        b.y = Mathf.Clamp(b.y, 0, b.y);
                                        gameObject.GetComponent<PlyMovementSync>().SetPosition(gameObject.transform.position + b);
                                    }
                                }

                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(3, $"<size=25>《SCP-173ブーストを使用しました。(HP消費:{usehp})》\n </size><size=20>《Used <SCP-173> boost.(used HP:{usehp})》\n</size>", false);
                                ev.ReturnMessage = $"Boost!(-{usehp})";
                            }
                            else
                            {
                                ev.ReturnMessage = "HPが足りません。";
                            }
                        }
                        else if(ev.Player.TeamRole.Role == Role.SCP_079)
                        {
                            if((ev.Player.Scp079Data.AP > 50 && scp079_boost_cooltime <= 0) || ev.Player.GetBypassMode())
                            {
                                UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
                                Scp079PlayerScript ply079 = gameObject.GetComponent<Scp079PlayerScript>();
                                Camera079 camera = ply079.currentCamera;
                                if(gameObject != null)
                                {
                                    if(camera.cameraName.Contains("ICOM"))
                                    {
                                        if(NineTailedFoxAnnouncer.singleton.isFree)
                                        {
                                            int number;
                                            char letter;
                                            NineTailedFoxUnits.host.NewName(out number, out letter);
                                            plugin.Server.Map.AnnounceNtfEntrance(plugin.Server.Round.Stats.SCPAlive, number, letter);

                                            if(!ev.Player.GetBypassMode())
                                            {
                                                ev.Player.Scp079Data.AP = Mathf.Clamp(ev.Player.Scp079Data.AP - 50f, 0, ev.Player.Scp079Data.AP - 50f);
                                                scp079_boost_cooltime = 30;
                                            }

                                            ev.ReturnMessage = "Fake MTF Announce.";
                                        }
                                        else
                                        {
                                            ev.ReturnMessage = "放送が使用中です。";
                                        }
                                    }
                                    else if(camera.cameraName.Contains("SCP-079 MAIN CAM"))
                                    {
                                        if(NineTailedFoxAnnouncer.singleton.isFree)
                                        {
                                            List<Player> plys = plugin.Server.GetPlayers().FindAll(x => x.TeamRole.Team == Smod2.API.Team.SCP && x.TeamRole.Role != Role.SCP_049_2 && x.TeamRole.Role != Role.SCP_079);
                                            if(plys.Count > 0)
                                            {
                                                var target = plys[rnd.Next(0, plys.Count)];
                                                if(target.TeamRole.Role == Role.SCP_939_53 || target.TeamRole.Role == Role.SCP_939_89)
                                                {

                                                    plugin.Server.Map.AnnounceScpKill("939", null);
                                                }
                                                else
                                                {
                                                    plugin.Server.Map.AnnounceScpKill(plys[rnd.Next(0, plys.Count)].TeamRole.Name.Replace("SCP-", ""), null);
                                                }
                                            }
                                            else
                                            {
                                                plugin.Server.Map.AnnounceScpKill("079", null);
                                            }

                                            if(!ev.Player.GetBypassMode())
                                            {
                                                ev.Player.Scp079Data.AP = Mathf.Clamp(ev.Player.Scp079Data.AP - 50f, 0, ev.Player.Scp079Data.AP - 50f);
                                                scp079_boost_cooltime = 30;
                                            }

                                            ev.ReturnMessage = "Fake SCP Announce.";
                                        }
                                        else
                                        {
                                            ev.ReturnMessage = "放送が使用中です。";
                                        }
                                    }
                                    else
                                    {
                                        Vector3 position = ply079.currentCamera.transform.position;
                                        SanyaPlugin.Explode(ev.Player, new Vector(position.x, position.y, position.z), true);

                                        if(!ev.Player.GetBypassMode())
                                        {
                                            ev.Player.Scp079Data.AP = Mathf.Clamp(ev.Player.Scp079Data.AP - 50f, 0, ev.Player.Scp079Data.AP - 50f);
                                            scp079_boost_cooltime = 30;
                                        }

                                        ev.ReturnMessage = "Flash!";
                                    }
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = $"APが足らないか、クールタイム中です。(CT残り:{scp079_boost_cooltime}秒)";
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = "ブーストが可能なSCPではありません。";
                        }

                        if(ev.Player.GetCurrentItem() != null && ev.Player.GetCurrentItem().ItemType == ItemType.DROPPED_9 && ev.Player.GetBypassMode())
                        {
                            GameObject gameObject = ev.Player.GetGameObject() as GameObject;
                            Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                            Vector3 forward = scp049sc.plyCam.transform.forward;
                            Vector3 position = scp049sc.plyCam.transform.position;

                            if(gameObject != null)
                            {
                                RaycastHit raycastHit;
                                if(Physics.Raycast(position, forward, out raycastHit, 500f, FallDamage.staticGroundMask))
                                {
                                    plugin.Debug($"9mm_RaycastHit:{raycastHit.transform.name}:{raycastHit.point}");

                                    //ServerMod2.API.SmodPlayer target = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                    //CharacterClassManager comp = raycastHit.transform.GetComponent<CharacterClassManager>();

                                    //if(target != null && comp != null)
                                    //{
                                    //    if(target.PlayerId != ev.Player.PlayerId && target.TeamRole.Team != Smod2.API.Team.SCP)
                                    //    {
                                    //        //(target.GetGameObject() as GameObject).GetComponent<Inventory>().ServerDropAll();
                                    //        //(ev.Player.GetGameObject() as GameObject).GetComponent<Handcuffs>().NetworkcuffTarget = raycastHit.transform.gameObject;

                                    //    }
                                    //}

                                    Door door = raycastHit.collider.GetComponentInParent<Door>();
                                    if(door != null)
                                    {
                                        plugin.Debug($"9mm_Door:{door.name}:{door.transform.position}");
                                        if(!door.moving.moving)
                                        {
                                            if(door.isOpen)
                                            {
                                                SanyaPlugin.ShowHitmarker(ev.Player);
                                                door.SetStateWithSound(false);
                                            }
                                            else
                                            {
                                                SanyaPlugin.ShowHitmarker(ev.Player);
                                                door.SetStateWithSound(true);
                                            }
                                        }
                                    }
                                }
                            }
                            ev.ReturnMessage = "MAGIC EFFECT!";
                        }
                    }
                    else
                    {
                        ev.ReturnMessage = "このコマンドはラウンド進行のみ使用可能です。";
                    }
                }
                else if(ev.Command.StartsWith("attack"))
                {
                    if(roundduring)
                    {
                        if(ev.Player.TeamRole.Role == Role.SCP_079)
                        {
                            if(ev.Player.Scp079Data.AP > 5 && scp079_attack_cooltime <= 0 || ev.Player.GetBypassMode())
                            {
                                UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
                                WeaponManager wpm = gameObject.GetComponent<WeaponManager>();
                                CharacterClassManager plychm = gameObject.GetComponent<CharacterClassManager>();
                                Scp079PlayerScript ply079 = gameObject.GetComponent<Scp079PlayerScript>();

                                if(gameObject != null && ply079 != null)
                                {
                                    RaycastHit raycastHit;
                                    Vector3 forward = ply079.currentCamera.targetPosition.transform.forward;
                                    Vector3 position = ply079.currentCamera.transform.position;

                                    if(Physics.Raycast(position + forward, forward, out raycastHit, 500f, SanyaPlugin.playermask))
                                    {
                                        plugin.Debug("Hit");
                                        ServerMod2.API.SmodPlayer ply = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                        CharacterClassManager comp = raycastHit.transform.GetComponent<CharacterClassManager>();
                                        HitboxIdentity hitbox = raycastHit.collider.GetComponent<HitboxIdentity>();

                                        if(comp != null && ply != null && hitbox != null)
                                        {
                                            if(ply.TeamRole.Team != Smod2.API.Team.SCP)
                                            {
                                                plugin.Debug("Hurt");
                                                gameObject.GetComponent<PlayerStats>().HurtPlayer(
                                                    new PlayerStats.HitInfo(
                                                        SanyaPlugin.GetDamageFromHitbox(hitbox, 5.0f * (ev.Player.Scp079Data.Level + 1)),
                                                        gameObject.GetComponent<NicknameSync>().myNick + " (" + plychm.SteamId + ")",
                                                        DamageTypes.Tesla,
                                                        gameObject.GetComponent<QueryProcessor>().PlayerId)
                                                    , raycastHit.transform.gameObject
                                                );
                                                raycastHit.transform.gameObject.GetComponent<FallDamage>().CallRpcDoSound(raycastHit.transform.position, 1.0f);
                                                SanyaPlugin.ShowHitmarker(ev.Player);
                                            }
                                        }
                                        wpm.CallRpcPlaceDecal(false, 0, raycastHit.point + raycastHit.normal * 0.01f, Quaternion.FromToRotation(Vector3.up, raycastHit.normal));
                                    }

                                    if(!ev.Player.GetBypassMode())
                                    {
                                        ev.Player.Scp079Data.AP -= 5f;
                                    }
                                }
                                ev.ReturnMessage = "079 Attack!";
                                scp079_attack_cooltime = 5;
                            }
                            else
                            {
                                ev.ReturnMessage = $"APが足らないか、クールタイム中です。(CT残り:{scp079_attack_cooltime}秒)";
                            }
                        }
                        else if(ev.Player.GetCurrentItem() != null && ev.Player.GetCurrentItem().ItemType == ItemType.DROPPED_9 && ev.Player.GetBypassMode())
                        {
                            GameObject gameObject = ev.Player.GetGameObject() as GameObject;
                            Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                            Vector3 forward = scp049sc.plyCam.transform.forward;
                            Vector3 position = scp049sc.plyCam.transform.position;
                            forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

                            if(gameObject != null)
                            {
                                RaycastHit raycastHit;
                                if(Physics.Raycast(position + forward, forward, out raycastHit, 500f, SanyaPlugin.playermask))
                                {
                                    plugin.Debug($"name:{raycastHit.transform.name} layer:{raycastHit.transform.gameObject.layer}");
                                    SanyaPlugin.Explode(ev.Player, new Vector(raycastHit.point.x, raycastHit.point.y, raycastHit.point.z),false,true);
                                }
                            }
                            ev.ReturnMessage = "MAGIC EFFECT!";
                        }
                        else if(ev.Player.GetCurrentItemIndex() == -1 && ev.Player.TeamRole.Team != Smod2.API.Team.SCP && ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR && !ev.Player.IsHandcuffed())
                        {
                            UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
                            CharacterClassManager plychm = gameObject.GetComponent<CharacterClassManager>();
                            Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                            Vector3 forward = scp049sc.plyCam.transform.forward;
                            Vector3 position = scp049sc.plyCam.transform.position;

                            forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

                            if(gameObject != null && forward != null && position != null)
                            {
                                RaycastHit raycastHit;
                                if(Physics.Raycast(position + forward, forward, out raycastHit, scp049sc.distance, SanyaPlugin.playermask))
                                {
                                    plugin.Debug("Hit");
                                    ServerMod2.API.SmodPlayer ply = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                    CharacterClassManager comp = raycastHit.transform.GetComponent<CharacterClassManager>();
                                    HitboxIdentity hitbox = raycastHit.collider.GetComponent<HitboxIdentity>();

                                    if(comp != null && ply != null && hitbox != null)
                                    {
                                        if(ev.Player.PlayerId == ply.PlayerId)
                                        {
                                            plugin.Debug("Self");
                                        }
                                        else if(ev.Player.TeamRole.Team != ply.TeamRole.Team || friendlyfire)
                                        {
                                            plugin.Debug("Hurt");
                                            raycastHit.transform.gameObject.GetComponent<FallDamage>().CallRpcDoSound(raycastHit.transform.position, SanyaPlugin.GetDamageFromHitbox(hitbox, 1.0f));

                                            Vector3 newvec = forward;
                                            Vector pos = ply.GetPosition();
                                            newvec.Scale(new Vector3(0.25f, 0.25f, 0.25f));
                                            Vector targetpos = new Vector(pos.x + newvec.x, pos.y, pos.z + newvec.z);
                                            Vector3 targetpos_3 = new Vector3(pos.x + newvec.x, pos.y, pos.z + newvec.z);
                                            if(!raycastHit.transform.gameObject.GetComponent<PlyMovementSync>().IsStuck(targetpos_3))
                                            {
                                                ply.Teleport(targetpos);
                                            }

                                            gameObject.GetComponent<PlayerStats>().HurtPlayer(
                                                new PlayerStats.HitInfo(
                                                    SanyaPlugin.GetDamageFromHitbox(hitbox, 1.0f),
                                                    gameObject.GetComponent<NicknameSync>().myNick + " (" + plychm.SteamId + ")",
                                                    DamageTypes.None,
                                                    gameObject.GetComponent<QueryProcessor>().PlayerId)
                                                , raycastHit.transform.gameObject
                                            );
                                            SanyaPlugin.ShowHitmarker(ev.Player);
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
                else if(ev.Command.StartsWith("test"))
                {
                    plugin.Warn($"[test_user] {ev.Player.Name}");
                    GameObject host = GameObject.Find("Host");
                    GameObject gameObject = ev.Player.GetGameObject() as GameObject;


                    //CharacterClassManager ccm = gameObject.GetComponent<CharacterClassManager>();
                    //PlayerStats.HitInfo info = new PlayerStats.HitInfo(1f, ev.Player.Name, DamageTypes.None, ev.Player.PlayerId);
                    //gameObject.GetComponent<RagdollManager>().SpawnRagdoll(gameObject.transform.position, gameObject.transform.rotation, ccm.curClass, info, false,
                    //    gameObject.GetComponent<Dissonance.Integrations.UNet_HLAPI.HlapiPlayer>().PlayerId, gameObject.GetComponent<NicknameSync>().myNick,
                    //    gameObject.GetComponent<RemoteAdmin.QueryProcessor>().PlayerId, gameObject
                    //);

                    //GrenadeManager gre = gameObject.GetComponent<GrenadeManager>();
                    //SanyaPlugin.Explode(ev.Player,ev.Player.GetPosition(),true);

                    //MicroHID_GFX hid = (ev.Player.GetGameObject() as GameObject).GetComponent<MicroHID_GFX>();
                    //if (hid != null)
                    //{
                    //    hid.CallRpcSyncAnim();
                    //}
                    //else
                    //{
                    //    plugin.Error("Hid NULL");
                    //}

                    //plugin.Error($"{CharacterClassManager.smRoundStartTime}");

                    //var decont = host.GetComponent<DecontaminationLCZ>();

                    //foreach(var i in decont.announcements)
                    //{
                    //    plugin.Error($"{i.startTime}");
                    //}

                    //plugin.Warn($"{decont.time}");

                    //decont.time = 11.1f * 60;

                    //ev.Player.SendConsoleMessage($"{ev.Player.Name} -> {10}","magenta");

                    //bool flag = false;
                    //foreach(var i in ev.Player.GetInventory())
                    //{
                    //    if(i.ItemType == ItemType.CUP)
                    //    {
                    //        flag = true;
                    //    }
                    //}

                    //if(flag)
                    //{
                    //    Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                    //    Vector3 forward = scp049sc.plyCam.transform.forward;
                    //    Vector3 position = scp049sc.plyCam.transform.position;
                    //    forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

                    //    if(gameObject != null)
                    //    {
                    //        RaycastHit raycastHit;
                    //        if(Physics.Raycast(position + forward, forward, out raycastHit, 500f, SanyaPlugin.playermask))
                    //        {
                    //            plugin.Debug($"name:{raycastHit.transform.name} layer:{raycastHit.transform.gameObject.layer}");
                    //            SanyaPlugin.Explode(ev.Player, new Vector(raycastHit.point.x, raycastHit.point.y, raycastHit.point.z));
                    //        }
                    //    }
                    //}

                    //AlphaWarheadController.host.InstantPrepare();
                    //AlphaWarheadController.host.NetworktimeToDetonation = 50f;

                    //foreach(var i in AlphaWarheadController.host.scenarios_start)
                    //{
                    //    plugin.Warn($"Start:{i.tMinusTime}:{i.additionalTime}:{i.SumTime()}");
                    //}

                    //foreach(var i in AlphaWarheadController.host.scenarios_resume)
                    //{
                    //    plugin.Warn($"Resume:{i.tMinusTime}:{i.additionalTime}:{i.SumTime()}");
                    //}

                    //AlphaWarheadController.host.timeToDetonation = 50f;
                    //AlphaWarheadController.host.Networksync_resumeScenario = 5;

                    //ev.Player.SetRadioBattery(10);

                    //SanyaPlugin.CallRpcTargetConfirmShoot(ev.Player);

                    //SanyaPlugin.CallRpcTargetDecontaminationLCZ(ev.Player);

                    //Timing.RunCoroutine(SanyaPlugin.JammingName(ev.Player),Segment.Update);

                    //var p106 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>();

                    //p106.CallRpcContainAnimation();

                    //Scp173PlayerScript ply173 = gameObject.GetComponent<Scp173PlayerScript>();
                    //Scp049PlayerScript ply049 = gameObject.GetComponent<Scp049PlayerScript>();

                    //foreach(Scp173PlayerScript scp173PlayerScript in GameObject.FindObjectsOfType<Scp173PlayerScript>())
                    //{
                    //    if(!scp173PlayerScript.iAm173)
                    //    {
                    //        scp173PlayerScript.CallRpcBlinkTime();
                    //    }
                    //}

                    //RaycastHit raycastHit;
                    //if(Physics.Raycast(ply049.plyCam.transform.position, ply049.plyCam.transform.forward, out raycastHit, 500f, ply173.teleportMask) && raycastHit.transform.GetComponent<CharacterClassManager>() != null)
                    //{
                    //    Vector3 b = raycastHit.transform.position - gameObject.transform.position;
                    //    b = b.normalized * Mathf.Clamp(b.magnitude - 1f, 0f, 500f);
                    //    gameObject.GetComponent<PlyMovementSync>().SetPosition(gameObject.transform.position + b);
                    //    var target = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                    //    if(target.PlayerId != ev.Player.PlayerId)
                    //    {
                    //        SanyaPlugin.ShowHitmarker(ev.Player);
                    //        SanyaPlugin.Call173SnapSound(target);
                    //        gameObject.GetComponent<PlayerStats>().HurtPlayer(
                    //            new PlayerStats.HitInfo(999990f, 
                    //            gameObject.GetComponent<NicknameSync>().myNick + " (" + gameObject.GetComponent<CharacterClassManager>().SteamId + ")", 
                    //            DamageTypes.Scp173, 
                    //            gameObject.GetComponent<QueryProcessor>().PlayerId), 
                    //            raycastHit.transform.gameObject);
                    //    }
                    //}
                    //else
                    //{
                    //    RaycastHit raycastHit2;
                    //    if(Physics.Raycast(ply049.plyCam.transform.position, ply049.plyCam.transform.forward, out raycastHit2, 500f, ply173.teleportMask))
                    //    {
                    //        Vector3 b = raycastHit2.point - gameObject.transform.position;
                    //        b = b.normalized * Mathf.Clamp(b.magnitude - 1f, 0f, 20.3f);
                    //        b.y = Mathf.Clamp(b.y, 0, b.y);
                    //        plugin.Error($"{b}");
                    //        gameObject.GetComponent<PlyMovementSync>().SetPosition(gameObject.transform.position + b);
                    //    }
                    //}
                    

                    ev.ReturnMessage = "test ok(user command)";
                }
            }

            plugin.Debug("[OnCallCommand] Called:" + ev.Player.Name + " Command:" + ev.Command + " Return:" + ev.ReturnMessage);
        }

        public void OnShoot(PlayerShootEvent ev)
        {
            plugin.Debug($"[OnShoot] {ev.Player.Name}[{ev.SourcePosition}({ev.Direction})] -> {ev.Target?.Name}{ev.TargetPosition}({ev.TargetHitbox}) [{ev.Weapon}:{ev.WeaponSound}] [{ev.ShouldSpawnHitmarker}:{ev.ShouldSpawnBloodDecal}]");


            //if (ev.Weapon == DamageType.USP)
            //{
            //    UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
            //    Inventory inv = gameObject.GetComponent<Inventory>();
            //    WeaponManager wpm = gameObject.GetComponent<WeaponManager>();
            //    Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
            //    Vector3 forward = scp049sc.plyCam.transform.forward;
            //    Vector3 position = scp049sc.plyCam.transform.position;
            //    forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

            //    if (gameObject != null && forward != null && position != null)
            //    {
            //        Ray[] rays = new Ray[8];
            //        for (int i = 0; i < rays.Length; i++)
            //        {
            //            rays[i] = new Ray(position + forward, Quaternion.Euler(UnityEngine.Random.Range(-3, 3), UnityEngine.Random.Range(-3, 3), UnityEngine.Random.Range(-3, 3)) * forward);
            //        }
            //        RaycastHit raycastHit;
            //        foreach (Ray i in rays)
            //        {
            //            if (Physics.Raycast(i, out raycastHit, 500f, SanyaPlugin.playermask))
            //            {
            //                plugin.Debug("Hit");
            //                ServerMod2.API.SmodPlayer ply = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
            //                HitboxIdentity hitbox = raycastHit.collider.GetComponent<HitboxIdentity>();
            //                CharacterClassManager ccm = raycastHit.transform.gameObject.GetComponent<CharacterClassManager>();
            //                float damage = SanyaPlugin.GetDamageFromHitbox(hitbox, wpm.weapons[wpm.curWeapon].damageOverDistance.Evaluate(Vector3.Distance(wpm.camera.transform.position, gameObject.transform.position)));
            //                damage *= wpm.weapons[wpm.curWeapon].allEffects.damageMultiplier;
            //                damage *= wpm.overallDamagerFactor;

            //                if (ply != null && hitbox != null)
            //                {
            //                    if (ev.Player.PlayerId == ply.PlayerId)
            //                    {
            //                        plugin.Debug("Self");
            //                    }
            //                    else if (ev.Player.TeamRole.Team != ply.TeamRole.Team || friendlyfire)
            //                    {
            //                        plugin.Debug("Hurt");
            //                        gameObject.GetComponent<PlayerStats>().HurtPlayer(
            //                            new PlayerStats.HitInfo(
            //                                damage,
            //                                gameObject.GetComponent<NicknameSync>().myNick + " (" + gameObject.GetComponent<CharacterClassManager>().SteamId + ")",
            //                                DamageTypes.Usp,
            //                                gameObject.GetComponent<QueryProcessor>().PlayerId)
            //                            , raycastHit.transform.gameObject
            //                        );
            //                    }
            //                    wpm.CallRpcPlaceDecal(true, ccm.klasy[ccm.curClass].bloodType, raycastHit.point + raycastHit.normal * 0.01f, Quaternion.FromToRotation(Vector3.up, raycastHit.normal));
            //                }
            //                else
            //                {
            //                    wpm.CallRpcPlaceDecal(false, wpm.curWeapon, raycastHit.point + raycastHit.normal * 0.01f, Quaternion.FromToRotation(Vector3.up, raycastHit.normal));
            //                }
            //            }
            //        }
            //    }
            //}
            //else if (ev.Weapon == DamageType.E11_STANDARD_RIFLE)
            //{
            //    UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
            //    Inventory inv = gameObject.GetComponent<Inventory>();
            //    WeaponManager wpm = gameObject.GetComponent<WeaponManager>();
            //    Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
            //    Vector3 forward = scp049sc.plyCam.transform.forward;
            //    Vector3 position = scp049sc.plyCam.transform.position;
            //    forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

            //    if (gameObject != null && forward != null && position != null)
            //    {
            //        RaycastHit raycastHit;
            //        if (Physics.Raycast(position + forward, forward, out raycastHit, 500f, SanyaPlugin.playermask))
            //        {
            //            plugin.Debug($"name:{raycastHit.transform.name} layer:{raycastHit.transform.gameObject.layer}");
            //            Door door = raycastHit.collider.GetComponentInParent<Door>();
            //            if (door != null && !door.destroyed)
            //            {
            //                door.DestroyDoor(true);
            //            }
            //            SanyaPlugin.Explode(ev.Player, new Vector(raycastHit.point.x, raycastHit.point.y, raycastHit.point.z));
            //        }
            //    }
            //    int index = inv.GetItemIndex();
            //    float dur = inv.items[index].durability;
            //    inv.items.ModifyDuration(index, 0f);
            //    System.Timers.Timer t = new System.Timers.Timer
            //    {
            //        Interval = 2000,
            //        AutoReset = false,
            //        Enabled = true
            //    };
            //    t.Elapsed += delegate
            //    {
            //        inv.items.ModifyDuration(index, dur);
            //        t.Enabled = false;
            //    };

            //    if (ev.Target != null)
            //    {
            //        SanyaPlugin.CallRpcTargetShake(ev.Target.GetGameObject() as GameObject);
            //        for (int i = 0; i < 15; i++)
            //        {
            //            wpm.CallRpcConfirmShot(true, (int)ev.WeaponSound.Value);
            //        }
            //    }
            //    else
            //    {
            //        for (int i = 0; i < 15; i++)
            //        {
            //            wpm.CallRpcConfirmShot(false, (int)ev.WeaponSound.Value);
            //        }
            //    }
            //}
        }
    }
}