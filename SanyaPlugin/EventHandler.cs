using System;
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

        public string rank { get; set; }
    }

    public class Serverinfo
    {
        public Serverinfo() { players = new List<Playerinfo>(); }

        public string time { get; set; }

        public string gameversion { get; set; }

        public string smodversion { get; set; }

        public string sanyaversion { get; set; }

        public string gamemode { get; set; }

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
        IEventHandlerPocketDimensionExit,
        IEventHandlerPocketDimensionDie,
        IEventHandlerInfected,
        IEventHandlerRecallZombie,
        IEventHandlerDoorAccess,
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
        IEventHandler079Door,
        IEventHandler079Elevator,
        IEventHandler079StartSpeaker,
        IEventHandler079Lockdown,
        IEventHandlerScp096Enrage,
        IEventHandlerSummonVehicle,
        IEventHandlerShoot,
        IEventHandlerTeamRespawn,
        IEventHandlerInitialAssignTeam,
        IEventHandlerWarheadChangeLever,
        IEventHandlerWarheadKeycardAccess,
        IEventHandlerIntercom,
        IEventHandlerGrenadeHitPlayer,
        IEventHandlerMedkitUse,
        IEventHandlerUpdate
    {
        internal readonly SanyaPlugin plugin;
        private UdpClient udpclient = new UdpClient();
        private System.Random rnd = new System.Random();
        private CoroutineHandle infosender;
        private bool disableSender = false;
        private bool running = false;

        //-----------------var---------------------
        //GlobalStatus
        private bool roundduring = false;
        public List<SCPPlayerData> scplist = new List<SCPPlayerData>();
        private bool isFirstSpawnFasted = false;

        //Eventer
        private SANYA_GAME_MODE eventmode = SANYA_GAME_MODE.NULL;
        //--HCZ--
        private List<Room> hcz_hallways = new List<Room>();
        private int fgamount = 0;
        private bool storyshaker = false;
        private int startwait = -1;
        //--CLASSDINS---
        private Vector camLCZarmory = null;
        private Vector roomUpstairs = null;
        //--STORY_173--
        private int chamber173ClassDcount = 0;
        private int hall173Scientistcount = 0;
        private Vector cam173hall = null;
        private int scp173amount = 0;
        private int scp079amount_story173 = 0;

        //--STORY_049--
        private int chamber049ClassDcount = 0;
        private int hall049Scientistcount = 0;
        private Vector cam049hall = null;
        private int scp049amount = 0;
        private int scp079amount_story049 = 0;

        //NightMode
        private int flickcount = 0;
        private int flickcount_lcz = 0;
        private bool gencomplete = false;

        //InventoryActer
        private Item[] itemtable = { };
        private List<string> genperm;

        //079LoneDeath
        private bool isAlreadyLoneDeath = false;

        //Dot
        static internal List<Player> dot_target = new List<Player>();

        //Update
        private int updatecounter = 0;
        private Vector traitor_pos = new Vector(170, 984, 28);
        private bool airbomb_used = false;
        private GameObject portal = null;

        //CommandCoolTime
        private Dictionary<int, int> playeridlist = new Dictionary<int, int>();

        //Command
        private int scp079_boost_cooltime = 0;
        private int scp079_attack_cooltime = 0;
        private int scp079_nuke_cooltime = 0;
        private int scp079_fake_announce_cooltime = 0;
        private int scp079_nukepanel_cooltime = 0;

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
                        disableSender = true;
                        break;
                    }
                    Serverinfo cinfo = new Serverinfo();
                    Server server = this.plugin.Server;

                    DateTime dt = DateTime.Now;
                    cinfo.time = dt.ToString("yyyy-MM-ddTHH:mm:sszzzz");
                    cinfo.gameversion = CustomNetworkManager.CompatibleVersions[0];
                    cinfo.smodversion = PluginManager.GetSmodVersion() + "-" + PluginManager.GetSmodBuild();
                    cinfo.sanyaversion = this.plugin.Details.version;
                    cinfo.gamemode = this.eventmode.ToString();
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
                            ply.rank = player.GetRankName();

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

            SanyaPlugin.roundStartTime = DateTime.Now;
            CharacterClassManager.smRoundStartTime = 0f;
            SanyaPlugin.SetExtraDoorNames();
            roundduring = false;
            updatecounter = 0;
            portal = null;

            if(plugin.data_enabled)
            {
                plugin.LoadPlayersData();
            }
            if(plugin.nuke_button_auto_close > 0f)
            {
                SanyaPlugin.SetExtraPermission("NUKE_SURFACE", "EXIT_ACC");
            }

            genperm = new List<string>(plugin.ConfigManager.Config.GetListValue("generator_keycard_perm", new string[] { "ARMORY_LVL_3" }, false));
            startwait = plugin.ConfigManager.Config.GetIntValue("173_door_starting_cooldown", 25) * 3;

            eventmode = (SANYA_GAME_MODE)SanyaPlugin.GetRandomIndexFromWeight(plugin.event_mode_weight);
            switch(eventmode)
            {
                case SANYA_GAME_MODE.NULL:
                    eventmode = SANYA_GAME_MODE.NORMAL;
                    break;
                case SANYA_GAME_MODE.STORY_173:
                    cam173hall = SanyaPlugin.GetCameraPosByName("173 HALLWAY") - new Vector(0, 2, 0);
                    break;
                case SANYA_GAME_MODE.STORY_049:
                    cam049hall = SanyaPlugin.GetCameraPosByName("049 HALL 5") - new Vector(0, 1, 0);
                    break;
                case SANYA_GAME_MODE.CLASSD_INSURGENCY:
                    camLCZarmory = SanyaPlugin.GetCameraPosByName("ARMORY") - new Vector(0, 1, 0);
                    roomUpstairs = SanyaPlugin.GetRoomPosByRoomid("Offices_upstair") + new Vector(0, 1, 0);
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
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            SanyaPlugin.roundStartTime = System.DateTime.Now;

            itemtable = GameObject.Find("Host").GetComponent<Inventory>().availableItems;
            roundduring = true;

            plugin.Info($"RoundStart! Eventmode:{eventmode}");
            plugin.Info($"Class-D:{plugin.Round.Stats.ClassDAlive} Scientist:{plugin.Round.Stats.ScientistsAlive} NTF:{plugin.Round.Stats.NTFAlive} SCP:{plugin.Round.Stats.SCPAlive} CI:{plugin.Round.Stats.CiAlive}");

            if(plugin.first_respawn_time_fast > 1.0f && !isFirstSpawnFasted)
            {
                Timing.RunCoroutine(SanyaPlugin._DelayedFastSpawn(plugin.first_respawn_time_fast), Segment.Update);
                isFirstSpawnFasted = true;
            }

            switch(eventmode)
            {
                case SANYA_GAME_MODE.NIGHT:
                    foreach(Generator079 gen in Generator079.generators)
                    {
                        gen.startDuration = plugin.night_generator_duration;
                        gen.NetworkremainingPowerup = gen.startDuration;
                    }
                    break;
                case SANYA_GAME_MODE.STORY_173:
                case SANYA_GAME_MODE.STORY_049:
                    foreach(Smod2.API.Player i in plugin.Server.GetPlayers(Smod2.API.Team.SCP).FindAll(x => x.TeamRole.Role != Role.SCP_049_2))
                    {
                        foreach(var x in plugin.Server.GetRoles(i.TeamRole.Name))
                        {
                            x.RoleDisallowed = false;
                        }
                        i.ChangeRole(Role.TUTORIAL);
                    }
                    break;
                case SANYA_GAME_MODE.CLASSD_INSURGENCY:
                    RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                    bool alreadyDisarmer = false;
                    bool alreadyMedkit = false;

                    foreach(var rpos in rnde.posIds)
                    {
                        if(rpos.posID == "LC_Armory")
                        {
                            Vector v = new Vector(rpos.position.position.x, rpos.position.position.y, rpos.position.position.z) + Vector.Up;
                            for(int x = 0; x < plugin.classd_ins_items; x++)
                            {
                                plugin.Server.Map.SpawnItem(ItemType.LOGICER, v, Vector.Zero);
                            }
                        }
                        else if(rpos.posID == "usp" && rpos.position.position.y < 50f && rpos.position.position.y > -50)
                        {
                            Vector v = new Vector(rpos.position.position.x, rpos.position.position.y, rpos.position.position.z) + Vector.Up;
                            for(int x = 0; x < plugin.classd_ins_items; x++)
                            {
                                plugin.Server.Map.SpawnItem(ItemType.CHAOS_INSURGENCY_DEVICE, v, Vector.Zero);
                            }
                        }
                        else if(rpos.posID == "LC_Armory_Smoke" && !alreadyDisarmer)
                        {
                            alreadyDisarmer = true;
                            Vector v = new Vector(rpos.position.position.x, rpos.position.position.y, rpos.position.position.z) + Vector.Up;
                            for(int x = 0; x < plugin.classd_ins_items; x++)
                            {
                                plugin.Server.Map.SpawnItem(ItemType.DISARMER, v, Vector.Zero);
                            }
                        }
                        else if(rpos.posID == "LC_Armory_Positron" && !alreadyMedkit)
                        {
                            alreadyMedkit = true;
                            Vector v = new Vector(rpos.position.position.x, rpos.position.position.y, rpos.position.position.z) + Vector.Up;
                            for(int x = 0; x < plugin.classd_ins_items; x++)
                            {
                                plugin.Server.Map.SpawnItem(ItemType.MEDKIT, v, Vector.Zero);
                            }
                        }
                    }
                    break;
            }
        }

        public void OnCheckRoundEnd(CheckRoundEndEvent ev)
        {
            if(plugin.ci_and_scp_noend)
            {
                if(ev.Status == ROUND_END_STATUS.SCP_CI_VICTORY)
                {
                    ev.Status = ROUND_END_STATUS.ON_GOING;
                }
            }
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if(roundduring)
            {
                plugin.Info($"Round Ended [{ev.Status}] Duration: {ev.Round.Duration / 60}:{ev.Round.Duration % 60}");
                plugin.Info($"Class-D:{ev.Round.Stats.ClassDAlive} Scientist:{ev.Round.Stats.ScientistsAlive} NTF:{ev.Round.Stats.NTFAlive} SCP:{ev.Round.Stats.SCPAlive} CI:{ev.Round.Stats.CiAlive}");

                SanyaPlugin.isAirBombGoing = false;

                if(plugin.endround_all_godmode)
                {
                    foreach(Player item in plugin.Server.GetPlayers())
                    {
                        item.SetGodmode(true);
                    }
                }

                if(plugin.level_enabled && plugin.data_enabled)
                {
                    foreach(Player player in plugin.Server.GetPlayers())
                    {
                        PlayerData curData = plugin.playersData.Find(x => x.steamid == player.SteamId);
                        if(curData != null)
                        {
                            if(player.TeamRole.Role == Role.SPECTATOR)
                            {
                                plugin.Debug($"[ExpAdd/RoundEnd/Other] {curData.exp}+={plugin.level_exp_other} / { curData.steamid}:{ player.Name}");
                                player.SendConsoleMessage($"[AddExp] +{plugin.level_exp_other}(Next:{Mathf.Clamp(curData.level * 3 - curData.exp + plugin.level_exp_other, 0, curData.level * 3 - curData.exp + plugin.level_exp_other)})");
                                curData.AddExp(plugin.level_exp_other, player);
                            }
                            else
                            {
                                plugin.Debug($"[ExpAdd/RoundEnd/Win] {curData.exp}+={plugin.level_exp_win} / {curData.steamid}:{ player.Name}");
                                player.SendConsoleMessage($"[AddExp] +{plugin.level_exp_win}(Next:{Mathf.Clamp(curData.level * 3 - curData.exp + plugin.level_exp_win, 0, curData.level * 3 - curData.exp + plugin.level_exp_win)})");
                                curData.AddExp(plugin.level_exp_win, player);
                            }
                        }
                        else
                        {
                            plugin.Error($"[ExpAdd/RoundEnd] Missing data,Passed...{player.SteamId}");
                        }
                    }
                    plugin.SavePlayersData();
                }
            }
            roundduring = false;
        }

        public void OnRoundRestart(RoundRestartEvent ev)
        {
            plugin.Info($"RoundRestart...");
            roundduring = false;
            hcz_hallways.Clear();
            playeridlist.Clear();
            scplist.Clear();
            gencomplete = false;
            chamber173ClassDcount = 0;
            hall173Scientistcount = 0;
            camLCZarmory = null;
            roomUpstairs = null;
            cam173hall = null;
            storyshaker = false;
            scp173amount = 0;
            scp079amount_story173 = 0;

            chamber049ClassDcount = 0;
            hall049Scientistcount = 0;
            cam049hall = null;
            scp049amount = 0;
            scp079amount_story049 = 0;

            isAlreadyLoneDeath = false;

            fgamount = 0;
            SanyaPlugin.scp_override_steamid = "";
            isFirstSpawnFasted = false;
            airbomb_used = false;
            SanyaPlugin.isAirBombGoing = false;
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            plugin.Info($"[PlayerJoin] {ev.Player.Name}[{ev.Player.IpAddress}]({ev.Player.SteamId})");

            //SteamLimitedCheck
            if(plugin.steam_kick_limited)
            {
                Timing.RunCoroutine(plugin._CheckIsLimitedSteam(ev.Player), Segment.FixedUpdate);
            }

            //Level
            if(plugin.data_enabled)
            {
                PlayerData curData = plugin.playersData.Find(x => x.steamid == ev.Player.SteamId);
                if(curData != null)
                {
                    plugin.Debug($"Already Exist.[{ev.Player.SteamId}:{curData.level}:{curData.exp}]");
                    if(plugin.level_enabled)
                    {
                        Timing.RunCoroutine(SanyaPlugin._DelayedGrantedLevel(ev.Player, curData), Segment.Update);
                    }
                }
                else
                {
                    plugin.Warn($"New SteamId.[{ev.Player.SteamId}]");
                    curData = new PlayerData(ev.Player.SteamId, true, 1, 0);
                    plugin.playersData.Add(curData);
                    plugin.SavePlayersData();
                    if(plugin.level_enabled)
                    {
                        Timing.RunCoroutine(SanyaPlugin._DelayedGrantedLevel(ev.Player, curData), Segment.Update);
                    }
                }
            }

            //MOTD
            if(plugin.motd_enabled)
            {
                if(!string.IsNullOrEmpty(plugin.motd_target_role) && ev.Player.GetRankName() == plugin.motd_target_role)
                {
                    ev.Player.PersonalClearBroadcasts();
                    ev.Player.PersonalBroadcast(10, $"{plugin.motd_role_message.Replace("[name]", ev.Player.Name)}", false);
                }
                else
                {
                    ev.Player.PersonalClearBroadcasts();
                    ev.Player.PersonalBroadcast(10, $"{plugin.motd_message.Replace("[name]", ev.Player.Name)}", false);
                }
            }

            //scplist
            if(plugin.scp_disconnect_at_resetrole && !plugin.Server.Map.WarheadDetonated)
            {
                var target = scplist.Find(x => x.name == ev.Player.Name);
                if(target != null)
                {
                    bool canreset = false;
                    if(target.role == Role.SCP_106 && !UnityEngine.Object.FindObjectOfType<OneOhSixContainer>().used)
                    {
                        canreset = true;
                    }
                    else if(target.role == Role.SCP_079 && Generator079.mainGenerator.totalVoltage < 5 && !isAlreadyLoneDeath)
                    {
                        canreset = true;
                    }
                    else if(target.role != Role.SCP_106 && target.role != Role.SCP_079)
                    {
                        canreset = true;
                    }

                    if(canreset)
                    {
                        plugin.Warn($"[SCPList/ReSet] {target.name}:{target.role} (HP:{target.health}/Tier:{target.level079}/AP:{target.ap079})");
                        target.id = ev.Player.PlayerId;
                        ev.Player.PersonalClearBroadcasts();
                        ev.Player.PersonalBroadcast(3, plugin.scp_rejoin_message, false);
                        Timing.RunCoroutine(SanyaPlugin._DelayedSetReSetRole(target, ev.Player), Segment.Update);
                    }
                }
            }

            //lobby
            if(plugin.waiting_for_match_spawn && !roundduring && running && !RoundSummary.RoundInProgress() && !PlayerManager.localPlayer.GetComponent<CharacterClassManager>().roundStarted)
            {
                ev.Player.ChangeRole(Role.TUTORIAL, true, false);
                Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, plugin.Server.Map.GetRandomSpawnPoint(Role.SCP_106), true), Segment.Update);
            }
        }

        public void OnDisconnect(DisconnectEvent ev)
        {
            plugin.Debug($"[OnDisconnect] {ev.Connection.IpAddress}/{ev.Connection.IsBanned}");

            //SCPList_Save
            if(plugin.scp_disconnect_at_resetrole && !ev.Connection.IsBanned)
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

            //LCZDecontaminated Subtitle
            if(plugin.cassie_subtitle && roundduring)
            {
                plugin.Server.Map.ClearBroadcasts();
                plugin.Server.Map.Broadcast(13, plugin.decontaminated_message, false);
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

            if(plugin.stop_mtf_after_nuke)
            {
                ChopperAutostart chop = UnityEngine.Object.FindObjectOfType<ChopperAutostart>();
                if(chop.isLanded)
                {
                    chop.SetState(false);
                }
            }
        }

        public void OnStartCountdown(WarheadStartEvent ev)
        {
            plugin.Debug($"[OnStartCountdown] {ev.Activator?.Name}:{ev.Cancel}:{ev.TimeLeft}");
            int autoleft = (int)Math.Truncate(AlphaWarheadController.host.smtimeToScheduledDetonation);
            double left = ev.IsResumed ? ev.TimeLeft : ev.TimeLeft - 4;
            double count = Math.Truncate(left / 10.0) * 10.0;

            if(!ev.Cancel && autoleft == 0 && AlphaWarheadController.host.smrealScheduledDetonationTime != -1 && !AlphaWarheadController.host.inProgress)
            {
                AlphaWarheadController.host.InstantPrepare();
                AlphaWarheadController.host.NetworkinProgress = true;
            }

            if(!ev.Cancel && AlphaWarheadController.host.inProgress)
            {
                if(plugin.nuke_start_countdown_door_lock)
                {
                    foreach(Smod2.API.Door door in plugin.Server.Map.GetDoors())
                    {
                        if(autoleft == 0 && AlphaWarheadController.host.smrealScheduledDetonationTime != -1)
                        {
                            if(plugin.nuke_start_countdown_door_lock && !door.Name.Contains("CHECKPOINT_") && !door.Name.Contains("AIRLOCK"))
                            {
                                door.Locked = true;
                                door.Open = true;
                            }
                        }
                        else
                        {
                            if(plugin.nuke_start_countdown_door_lock && !door.Name.Contains("GATE_") && !door.Name.Contains("106_") && !door.Name.Contains("CHECKPOINT_") && !door.Name.Contains("AIRLOCK"))
                            {
                                door.Locked = true;
                                door.Open = true;
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
                            plugin.Server.Map.Broadcast(15, plugin.alphawarhead_countdown_start_message.Replace("[name]", ev.Activator.Name).Replace("[team]", ev.Activator.TeamRole.Team.ToString()).Replace("[second]", count.ToString()), false);
                        }
                        else
                        {
                            plugin.Server.Map.Broadcast(15, plugin.alphawarhead_countdown_start_server_message.Replace("[second]", count.ToString()), false);
                        }
                    }
                    else
                    {
                        plugin.Server.Map.ClearBroadcasts();
                        if(ev.Activator != null)
                        {
                            plugin.Server.Map.Broadcast(10, plugin.alphawarhead_countdown_resume_message.Replace("[name]", ev.Activator.Name).Replace("[team]", ev.Activator.TeamRole.Team.ToString()).Replace("[second]", count.ToString()), false);
                        }
                        else
                        {
                            plugin.Server.Map.Broadcast(10, plugin.alphawarhead_countdown_resume_server_message.Replace("[second]", count.ToString()), false);
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
                        if(plugin.nuke_start_countdown_door_lock && !door.Name.Contains("GATE_") && !door.Name.Contains("106_"))
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
                        plugin.Server.Map.Broadcast(7, plugin.alphawarhead_countdown_stop_message.Replace("[name]", ev.Activator.Name).Replace("[team]", ev.Activator.TeamRole.Team.ToString()), false);
                    }
                    else
                    {
                        plugin.Server.Map.Broadcast(7, plugin.alphawarhead_countdown_stop_server_message, false);
                    }
                }
            }
        }

        public void OnChangeLever(WarheadChangeLeverEvent ev)
        {
            plugin.Debug($"[OnChangeLever] {ev.Player.Name}:{ev.Allow}:{AlphaWarheadOutsitePanel.nukeside.enabled}");
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
                        plugin.Server.Map.Broadcast(30, plugin.mtfspawn_message.Replace("[unit]", ev.Unit).Replace("[amount]", plugin.Server.Round.Stats.SCPAlive.ToString()), false);
                    }
                    else
                    {
                        plugin.Server.Map.Broadcast(30, plugin.mtfspawn_noscp_message.Replace("[unit]", ev.Unit), false);
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
                plugin.Info($"[EscapeSpawn] Escaped:{ev.Player.Name}/{ev.Player.TeamRole.Role}:isCuffed({ev.Player.IsHandcuffed()})");
                (ev.Player.GetGameObject() as GameObject).GetComponent<Inventory>().ServerDropAll();
                if((ev.Player.TeamRole.Role == Role.CLASSD && ev.Player.IsHandcuffed()) || (ev.Player.TeamRole.Role == Role.SCIENTIST && !ev.Player.IsHandcuffed()))
                {
                    plugin.Debug($"Call Teleport(MTF)");
                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, ev.Player.GetPosition(), false), Segment.Update);
                }
                else
                {
                    plugin.Debug($"Call Teleport(CI)");
                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, new Vector(-55, 988, -49), false), Segment.Update);
                }
            }

            if(plugin.classd_escaped_additemid != -1 && ev.Player.TeamRole.Role == Role.CLASSD && !ev.Player.IsHandcuffed())
            {
                Timing.RunCoroutine(SanyaPlugin._DelayedAddItem(ev.Player, (ItemType)plugin.classd_escaped_additemid), Segment.Update);
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

            switch(eventmode)
            {
                case SANYA_GAME_MODE.HCZ:
                    if((ev.Team == Smod2.API.Team.NINETAILFOX || ev.Team == Smod2.API.Team.CHAOS_INSURGENCY) && fgamount < plugin.hczstart_mtf_and_ci)
                    {
                        fgamount++;
                        plugin.Info($"[OnAssignTeam] {ev.Player.Name} {ev.Team}->SCIENTIST({eventmode})");
                        ev.Team = Smod2.API.Team.SCIENTIST;
                    }
                    break;
            }
        }

        public void OnSetRole(PlayerSetRoleEvent ev)
        {
            if(ev.Player.IpAddress == "localClient" || ev.Player.TeamRole.Role == Role.UNASSIGNED) return;
            plugin.Debug($"[OnSetRole] {ev.Player.Name}:{ev.Role}");

            //scplist
            if(plugin.scp_disconnect_at_resetrole)
            {
                var target = scplist.Find(x => x.name == ev.Player.Name);
                if(target == null && ev.TeamRole.Team == Smod2.API.Team.SCP && ev.Player.TeamRole.Role != Role.SCP_049_2)
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
            if(RoundSummary.RoundInProgress())
            {
                switch(eventmode)
                {
                    case SANYA_GAME_MODE.STORY_173:
                        if(cam173hall != null && ev.Role == Role.CLASSD && chamber173ClassDcount < 4)
                        {
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, cam173hall, false), Segment.Update);
                            chamber173ClassDcount++;
                        }

                        if(cam173hall != null && ev.Role == Role.SCIENTIST && hall173Scientistcount < 4)
                        {
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, cam173hall, false), Segment.Update);
                            hall173Scientistcount++;
                        }

                        if(ev.Role == Role.TUTORIAL)
                        {
                            if(scp173amount == 0)
                            {
                                plugin.Info($"[Eventer:STORY_173] SCP-173:{ev.Player.Name}");
                                ev.Role = Role.SCP_173;
                                scp173amount++;
                            }
                            else if(scp079amount_story173 == 0)
                            {
                                plugin.Info($"[Eventer:STORY_173] SCP-079:{ev.Player.Name}");
                                ev.Role = Role.SCP_079;
                                ev.Player.Scp079Data.SetCamera(SanyaPlugin.GetCameraPosByName("173 CHAMBER"));
                                scp079amount_story173++;
                                plugin.Info($"[Eventer:STORY_173] OtherSCP({ev.Role}):{ev.Player.Name}");
                            }
                            else
                            {
                                List<Role> scpqueue = new List<Role>();
                                foreach(var i in plugin.Server.GetRoles("SCP"))
                                {
                                    if(!i.RoleDisallowed && i.Role != Role.SCP_173 && i.Role != Role.SCP_079 && i.Role != Role.SCP_049_2)
                                    {
                                        plugin.Debug($"add queue:{i.Role}");
                                        scpqueue.Add(i.Role);
                                    }
                                }
                                if(scpqueue.Count != 0)
                                {
                                    ev.Role = scpqueue[rnd.Next(0, scpqueue.Count)];
                                    plugin.Server.GetRoles("SCP").Find(x => x.Role == ev.Role).RoleDisallowed = true;
                                }
                                else
                                {
                                    plugin.Error($"[Eventer:STORY_173] No SCP Queues,Skipped...({ev.Role}):{ev.Player.Name}");
                                }


                                //Other SCP = 372CONTAIN
                                Vector containpos = null;
                                RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                                foreach(var itempos in rnde.posIds)
                                {
                                    if(itempos.posID == "RandomPistol" && itempos.position.position.y > 0.5f && itempos.position.position.y < 0.7f)
                                    {
                                        containpos = new Vector(itempos.position.position.x, itempos.position.position.y + 1, itempos.position.position.z);
                                    }
                                }
                                if(containpos != null)
                                {
                                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, containpos, false), Segment.Update);
                                }
                            }
                        }
                        break;
                    case SANYA_GAME_MODE.STORY_049:
                        if(ev.Role == Role.CLASSD && chamber049ClassDcount < 4)
                        {
                            ev.Role = Role.SCP_049_2;
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, plugin.Server.Map.GetRandomSpawnPoint(Role.SCP_049), false), Segment.Update);
                            chamber049ClassDcount++;
                        }

                        if(cam049hall != null && ev.Role == Role.SCIENTIST && hall049Scientistcount < 4)
                        {
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, cam049hall, false), Segment.Update);
                            hall049Scientistcount++;
                        }

                        if(ev.Role == Role.TUTORIAL)
                        {
                            if(scp049amount == 0)
                            {
                                plugin.Info($"[Eventer:STORY_049] SCP-049:{ev.Player.Name}");
                                ev.Role = Role.SCP_049;
                                scp049amount++;
                            }
                            else if(scp079amount_story049 == 0)
                            {
                                plugin.Info($"[Eventer:STORY_049] SCP-079:{ev.Player.Name}");
                                ev.Role = Role.SCP_079;
                                ev.Player.Scp079Data.SetCamera(SanyaPlugin.GetCameraPosByName("049 HALL 5"));
                                scp079amount_story049++;
                            }
                            else
                            {
                                List<Role> scpqueue = new List<Role>();
                                foreach(var i in plugin.Server.GetRoles("SCP"))
                                {
                                    if(!i.RoleDisallowed && i.Role != Role.SCP_049 && i.Role != Role.SCP_079 && i.Role != Role.SCP_049_2)
                                    {
                                        plugin.Debug($"add queue:{i.Role}");
                                        scpqueue.Add(i.Role);
                                    }
                                }
                                if(scpqueue.Count != 0)
                                {
                                    ev.Role = scpqueue[rnd.Next(0, scpqueue.Count)];
                                    plugin.Server.GetRoles("SCP").Find(x => x.Role == ev.Role).RoleDisallowed = true;
                                    plugin.Info($"[Eventer:STORY_049] OtherSCP({ev.Role}):{ev.Player.Name}");
                                }
                                else
                                {
                                    plugin.Error($"[Eventer:STORY_049] No SCP Queues,Skipped...({ev.Role}):{ev.Player.Name}");
                                }


                                //Other SCP = 372CONTAIN
                                Vector containpos = null;
                                RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                                foreach(var itempos in rnde.posIds)
                                {
                                    if(itempos.posID == "RandomPistol" && itempos.position.position.y > 0.5f && itempos.position.position.y < 0.7f)
                                    {
                                        containpos = new Vector(itempos.position.position.x, itempos.position.position.y + 1, itempos.position.position.z);
                                    }
                                }
                                if(containpos != null)
                                {
                                    Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, containpos, false), Segment.Update);
                                }
                            }
                        }
                        break;
                    case SANYA_GAME_MODE.CLASSD_INSURGENCY:
                        if(camLCZarmory != null && ev.Role == Role.CLASSD)
                        {
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, camLCZarmory, false), Segment.Update);
                        }
                        if(roomUpstairs != null && ev.Role == Role.SCIENTIST)
                        {
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, roomUpstairs, false), Segment.Update);
                        }
                        break;
                    case SANYA_GAME_MODE.HCZ:
                        Room room = hcz_hallways[rnd.Next(0, hcz_hallways.Count)];
                        Vector pos = new Vector(room.Position.x, room.Position.y + 3, room.Position.z);

                        if(hcz_hallways != null && (ev.Role == Role.CLASSD || ev.Role == Role.SCIENTIST))
                        {
                            plugin.Debug($"HCZ Teleport:{room.RoomType} : {pos}");
                            Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ev.Player, pos, false), Segment.Update);
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
                    break;
                case Role.CHAOS_INSURGENCY:
                    targetammo = plugin.default_ammo_ci;
                    break;
                case Role.NTF_SCIENTIST:
                    targetammo = plugin.default_ammo_ntfscientist;
                    break;
                case Role.NTF_LIEUTENANT:
                    targetammo = plugin.default_ammo_lieutenant;
                    break;
                case Role.NTF_COMMANDER:
                    targetammo = plugin.default_ammo_commander;
                    break;
                case Role.NTF_CADET:
                    targetammo = plugin.default_ammo_cadet;
                    break;
            }
            ev.Player.SetAmmo(AmmoType.DROPPED_5, targetammo[(int)AmmoType.DROPPED_5]);
            ev.Player.SetAmmo(AmmoType.DROPPED_7, targetammo[(int)AmmoType.DROPPED_7]);
            ev.Player.SetAmmo(AmmoType.DROPPED_9, targetammo[(int)AmmoType.DROPPED_9]);
            plugin.Debug($"[SetAmmo] {ev.Player.Name}({ev.Role}) 5.56mm:{targetammo[(int)AmmoType.DROPPED_5]} 7.62mm:{targetammo[(int)AmmoType.DROPPED_7]} 9mm:{targetammo[(int)AmmoType.DROPPED_9]}");
        }

        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            if(ev.Player.IpAddress == "localClient" || ev.Player.TeamRole.Role == Role.UNASSIGNED) return;
            plugin.Debug($"[OnPlayerHurt] Before {ev.Attacker.Name}<{ev.Attacker.TeamRole.Role}>({ev.DamageType}:{ev.Damage}) -> {ev.Player.Name}<{ev.Player.TeamRole.Role}>");

            //さにゃぱっち
            //SCP-106の攻撃でSCP-079が攻撃した際にEXPを得られるように
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
                        ev.Attacker.PersonalBroadcast(3, plugin.friendlyfire_message.Replace("[name]", ev.Player.Name), false);
                    }
                    if(plugin.friendly_warn_console)
                    {
                        ev.Attacker.SendConsoleMessage($"FriendlyFire to {ev.Player.Name}({ev.DamageType}:{(int)Math.Truncate(ev.Damage)})", "magenta");
                        ev.Player.SendConsoleMessage($"FriendlyFire from {ev.Attacker.Name}({ev.DamageType}:{(int)Math.Truncate(ev.Damage)})", "yellow");
                    }
                }
            }

            if(plugin.scp096_damage_trigger && ev.Player.TeamRole.Role == Role.SCP_096)
            {
                if(Scp096PlayerScript.instance != null)
                {
                    Scp096PlayerScript.instance.IncreaseRage(1f);
                }
            }

            if(plugin.scp173_hurt_blink_percent > 0 && ev.Player.TeamRole.Role == Role.SCP_173)
            {
                int pers = rnd.Next(0, 100);
                int blinkpers = Mathf.Clamp(plugin.scp173_hurt_blink_percent, 0, 100);
                if(blinkpers > pers)
                {
                    plugin.Debug($"[SCP-173] HurtBlinked (percent:{pers}/{blinkpers})");
                    SanyaPlugin.Call173Blink();
                }
            }

            if(SanyaPlugin.isAirBombGoing && ev.Player.TeamRole.Team == Smod2.API.Team.SCP && ev.DamageType == DamageType.FRAG && ev.Attacker.PlayerId == ev.Player.PlayerId)
            {
                ev.Damage *= plugin.outsidezone_termination_multiplier_scp;
            }

            //SCP-939の出血DOT
            if(plugin.scp939_dot_damage > 0 && ev.DamageType == DamageType.SCP_939 && dot_target.Find(x => x.PlayerId == ev.Player.PlayerId) == null && ev.Player.GetHealth() - ev.Damage >= 0)
            {
                dot_target.Add(ev.Player);
                Timing.RunCoroutine(SanyaPlugin._DOTDamage(ev.Player, plugin.scp939_dot_damage, plugin.scp939_dot_damage_interval, plugin.scp939_dot_damage_total, DamageType.SCP_939), Segment.Update);
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

            //ロール除算計算開始
            if(ev.DamageType != DamageType.NUKE && ev.DamageType != DamageType.DECONT && ev.DamageType != DamageType.TESLA) //核と下層ロックとテスラ/HIDは対象外
            {
                switch(ev.Player.TeamRole.Role)
                {
                    case Role.SCP_173:
                        ev.Damage /= plugin.damage_divisor_scp173;
                        break;
                    case Role.SCP_106:
                        if(ev.DamageType == DamageType.FRAG)
                        {
                            ev.Damage /= plugin.damage_divisor_scp106_grenade;
                        }
                        ev.Damage /= plugin.damage_divisor_scp106;
                        break;
                    case Role.SCP_049:
                        ev.Damage /= plugin.damage_divisor_scp049;
                        break;
                    case Role.SCP_096:
                        ev.Damage /= plugin.damage_divisor_scp096;
                        break;
                    case Role.SCP_049_2:
                        ev.Damage /= plugin.damage_divisor_scp049_2;
                        break;
                    case Role.SCP_939_53:
                    case Role.SCP_939_89:
                        ev.Damage /= plugin.damage_divisor_scp939;
                        break;
                }

                if(ev.Player.IsHandcuffed())
                {
                    ev.Damage /= plugin.damage_divisor_cuffed;
                }
            }

            plugin.Debug($"[OnPlayerHurt] After {ev.Attacker.Name}<{ev.Attacker.TeamRole.Role}>({ev.DamageType}:{ev.Damage}) -> {ev.Player.Name}<{ev.Player.TeamRole.Role}>");
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            if(ev.Player.IpAddress == "localClient" || ev.Player.TeamRole.Role == Role.UNASSIGNED) return;
            plugin.Debug($"[OnPlayerDie] {ev.Killer}<{ev.Killer?.TeamRole.Role}>:{ev.DamageTypeVar} -> {ev.Player.Name}<{ev.Player?.TeamRole.Role}>");

            //LevelExp
            if(plugin.level_enabled && plugin.data_enabled)
            {
                if(ev.Killer.IpAddress != "localClient" && ev.Killer.TeamRole.Role != Role.UNASSIGNED && !string.IsNullOrEmpty(ev.Killer.SteamId))
                {
                    PlayerData curDataKiller = plugin.playersData.Find(x => x.steamid == ev.Killer.SteamId);
                    if(curDataKiller != null)
                    {
                        plugin.Debug($"[ExpAdd/Kill] {curDataKiller.exp}+={plugin.level_exp_kill} / {curDataKiller.steamid}:{ev.Killer.Name}");
                        ev.Killer.SendConsoleMessage($"[AddExp] +{plugin.level_exp_kill}(Next:{Mathf.Clamp(curDataKiller.level * 3 - curDataKiller.exp + plugin.level_exp_kill, 0, curDataKiller.level * 3 - curDataKiller.exp + plugin.level_exp_kill)})");
                        curDataKiller.AddExp(plugin.level_exp_kill, ev.Killer);
                    }
                    else
                    {
                        plugin.Error($"[ExpAdd/Kill] Missing Data,Passed...[{ev.Killer.SteamId}]");
                    }
                }

                if(ev.Player.IpAddress != "localClient" && ev.Player.TeamRole.Role != Role.UNASSIGNED && !string.IsNullOrEmpty(ev.Player.SteamId))
                {
                    PlayerData curDataDeath = plugin.playersData.Find(x => x.steamid == ev.Player.SteamId);
                    if(curDataDeath != null)
                    {
                        plugin.Debug($"[ExpAdd/Death] {curDataDeath.exp}+={plugin.level_exp_death} / {curDataDeath.steamid}:{ev.Player.Name}");
                        ev.Killer.SendConsoleMessage($"[AddExp] +{plugin.level_exp_death}(Next:{Mathf.Clamp(curDataDeath.level * 3 - curDataDeath.exp + plugin.level_exp_death, 0, curDataDeath.level * 3 - curDataDeath.exp + plugin.level_exp_death)})");
                        curDataDeath.AddExp(plugin.level_exp_death, ev.Player);
                    }
                    else
                    {
                        plugin.Error($"[ExpAdd/Death] Missing Data,Passed...[{ev.Player.SteamId}]");
                    }
                }
            }

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

            //----------------SCP-939の死体削除-----
            if(plugin.scp939_killed_ragdoll_clean
                && ev.DamageTypeVar == DamageType.SCP_939
                && (ev.Killer.TeamRole.Role == Role.SCP_939_53 || ev.Killer.TeamRole.Role == Role.SCP_939_89))
            {
                ev.SpawnRagdoll = false;
            }
            //-----------------SCP-939のキル時加速----
            if(plugin.scp939_killed_speedup_multiplier > 0
                && ev.DamageTypeVar == DamageType.SCP_939
                && (ev.Killer.TeamRole.Role == Role.SCP_939_53 || ev.Killer.TeamRole.Role == Role.SCP_939_89))
            {
                Scp939PlayerScript scp939ply = (ev.Killer.GetGameObject() as GameObject).GetComponent<Scp939PlayerScript>();
                scp939ply.NetworkspeedMultiplier = Mathf.Clamp(plugin.scp939_killed_speedup_multiplier, 1.0f, plugin.scp939_killed_speedup_multiplier);
            }

            //----------------------------------------------------キル回復------------------------------------------------  
            //############## SCP-173 ###############
            if(ev.DamageTypeVar == DamageType.SCP_173 &&
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
                plugin.Debug($"[Infector] 049-2 Infected!(Limit:{plugin.infect_limit_time}s) [{ev.Killer.Name}->{ev.Player.Name}]");
                ev.DamageTypeVar = DamageType.SCP_049;
                ev.Player.Infect(plugin.infect_limit_time);
            }
            //----------------------------------------------------ペスト治療------------------------------------------------

            //-------079ラストdeath-----
            if(plugin.scp079_lone_death)
            {
                if(!isAlreadyLoneDeath)
                {
                    if(ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                    {
                        bool iswithoutscp079 = false;
                        foreach(Smod2.API.Player player in plugin.Server.GetPlayers())
                        {
                            if(player.TeamRole.Team == Smod2.API.Team.SCP && player.TeamRole.Role != Role.SCP_079 && player.TeamRole.Role != Role.SCP_049_2 && player.PlayerId != ev.Player.PlayerId)
                            {
                                iswithoutscp079 = true;
                            }
                        }

                        if(!iswithoutscp079)
                        {
                            foreach(Smod2.API.Player player in plugin.Server.GetPlayers(Role.SCP_079))
                            {
                                plugin.Info($"[079lonedeath] {player.Name}");
                                Timing.RunCoroutine(SanyaPlugin._Emulate079Recontain(), Segment.FixedUpdate);
                                isAlreadyLoneDeath = true;
                                foreach(var i in plugin.Server.Map.GetGenerators())
                                {
                                    i.Open = false;
                                }
                            }
                        }
                    }
                }
            }

            //-------079ラストboost-----
            if(plugin.scp079_lone_boost && !plugin.scp079_lone_death)
            {
                if(ev.Player.TeamRole.Team == Smod2.API.Team.SCP)
                {
                    bool iswithoutscp079 = false;
                    foreach(Smod2.API.Player player in plugin.Server.GetPlayers())
                    {
                        if(player.TeamRole.Team == Smod2.API.Team.SCP && player.TeamRole.Role != Role.SCP_079 && player.TeamRole.Role != Role.SCP_049_2 && player.PlayerId != ev.Player.PlayerId)
                        {
                            iswithoutscp079 = true;
                        }
                    }

                    if(!iswithoutscp079)
                    {
                        foreach(Smod2.API.Player player in plugin.Server.GetPlayers(Role.SCP_079))
                        {
                            plugin.Info($"[079loneboost] {player.Name}");
                            player.Scp079Data.Level = 4;
                            player.Scp079Data.ShowLevelUp(4);
                        }
                    }
                }
            }

            if(plugin.cassie_subtitle && ev.Player.TeamRole.Team == Smod2.API.Team.SCP && ev.Player.TeamRole.Role != Role.SCP_049_2)
            {
                plugin.Server.Map.ClearBroadcasts();
                if(ev.Killer.TeamRole.Team == Smod2.API.Team.NINETAILFOX)
                {
                    string unit = GameObject.Find("Host").GetComponent<NineTailedFoxUnits>().GetNameById((ev.Killer.GetGameObject() as GameObject).GetComponent<CharacterClassManager>().ntfUnit);

                    plugin.Server.Map.Broadcast(10, plugin.scp_containment_message.Replace("[role]", ev.Player.TeamRole.Name).Replace("[unit]", unit).Replace("[name]", ev.Killer.Name), false);
                }
                else
                {
                    if(ev.DamageTypeVar == DamageType.TESLA && ev.Killer.TeamRole.Team == Smod2.API.Team.SCP)
                    {
                        plugin.Server.Map.Broadcast(10, plugin.scp_containment_tesla_message.Replace("[role]", ev.Player.TeamRole.Name), false);
                    }
                    else if(ev.DamageTypeVar == DamageType.NUKE)
                    {
                        plugin.Server.Map.Broadcast(10, plugin.scp_containment_alphawarhead_message.Replace("[role]", ev.Player.TeamRole.Name), false);
                    }
                    else if(ev.DamageTypeVar == DamageType.DECONT)
                    {
                        plugin.Server.Map.Broadcast(10, plugin.scp_containment_decont_message.Replace("[role]", ev.Player.TeamRole.Name), false);
                    }
                    else
                    {
                        plugin.Server.Map.Broadcast(10, plugin.scp_containment_unknown_message.Replace("[role]", ev.Player.TeamRole.Name).Replace("[name]", ev.Killer.Name), false);
                    }
                }
            }
        }

        public void OnMedkitUse(PlayerMedkitUseEvent ev)
        {
            plugin.Debug($"[OnMedkitUse] {ev.Player.Name}:{ev.RecoverHealth}");

            if(plugin.medkit_stop_dot_damage)
            {
                Player target = dot_target.Find(x => x.PlayerId == ev.Player.PlayerId);
                if(target != null)
                {
                    dot_target.Remove(target);
                }
            }

            if(plugin.scp106_pocket_medkit_recovery_amount > 0 && ev.Player.GetPosition().y < -1900 && ev.Player.GetPosition().y > -2000)
            {
                int amount = Mathf.Clamp(plugin.scp106_pocket_medkit_recovery_amount, 0, plugin.scp106_pocket_medkit_recovery_amount);
                plugin.Debug($"[pocketMedkitDecrase] {amount}");
                ev.RecoverHealth = amount;
            }
        }

        public void OnGrenadeHitPlayer(PlayerGrenadeHitPlayer ev)
        {
            plugin.Debug($"[OnGrenadeHitPlayer] {ev.Player?.Name}:{ev.Victim?.Name}");

            if(plugin.grenade_hitmark)
            {
                if(ev.Player != null && ev.Victim != null && ev.Player.PlayerId != ev.Victim.PlayerId)
                {
                    SanyaPlugin.ShowHitmarker(ev.Player);
                }
            }
        }

        public void OnPlayerInfected(PlayerInfectedEvent ev)
        {
            plugin.Debug($"[Infector] 049 Infected!(Limit:{plugin.infect_limit_time}s) [{ev.Attacker.Name}->{ev.Player.Name}]");
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

            if(plugin.scp106_hitmark_pocket_death)
            {
                foreach(Player ply in plugin.Server.GetPlayers(Role.SCP_106))
                {
                    SanyaPlugin.ShowHitmarker(ply);
                }
            }
        }

        public void OnPocketDimensionExit(PlayerPocketDimensionExitEvent ev)
        {
            plugin.Debug($"[OnPocketDimensionExit] {ev.Player.Name}:{ev.ExitPosition}");
        }

        public void OnRecallZombie(PlayerRecallZombieEvent ev)
        {
            plugin.Debug($"[OnRecallZombie] {ev.Player.Name} -> {ev.Target.Name} ({ev.AllowRecall})");

            if(ev.AllowRecall)
            {
                if(plugin.scp049_healing_to_other_scp_range > 0 && plugin.scp049_healing_to_other_scp_amount > 0)
                {
                    foreach(Player otherscp in plugin.Server.GetPlayers(Smod2.API.Team.SCP).FindAll(x => x.TeamRole.Role != Role.SCP_049))
                    {
                        int curHealth = (otherscp.GetGameObject() as GameObject).GetComponent<PlayerStats>().health;
                        if(curHealth >= otherscp.TeamRole.MaxHP) continue;

                        float distance = Vector.Distance(ev.Player.GetPosition(), otherscp.GetPosition());
                        if(plugin.scp049_healing_to_other_scp_range > distance)
                        {
                            plugin.Debug($"[049nearheal] {ev.Player.Name} -({distance})-> {otherscp.Name}");
                            otherscp.SetHealth(Mathf.Clamp(curHealth + plugin.scp049_healing_to_other_scp_amount, 0, otherscp.TeamRole.MaxHP), DamageType.NONE);
                        }
                    }
                }

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
                if(plugin.cassie_subtitle)
                {
                    ev.Player.PersonalClearBroadcasts();
                    ev.Player.PersonalBroadcast(3, plugin.scp049_recall_failed, false);
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
                case SANYA_GAME_MODE.STORY_173:
                case SANYA_GAME_MODE.STORY_049:
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

        public void OnElevatorUse(PlayerElevatorUseEvent ev)
        {
            plugin.Debug($"[OnElevatorUse] {ev.Player.Name} : {ev.Elevator.ElevatorType}[{ev.Elevator.ElevatorStatus}] - {ev.Elevator.MovingSpeed} : {ev.AllowUse}");

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
            plugin.Info($"[TeamRespawn] {ev.PlayerList.Count}/IsCI:{ev.SpawnChaos}");

            if(plugin.stop_mtf_after_nuke && (plugin.Server.Map.WarheadDetonated || AlphaWarheadController.host.inProgress) || !roundduring)
            {
                ev.PlayerList.Clear();
            }

            if(plugin.mtf_and_ci_change_spawnpoint > 0 && ev.PlayerList.Count > 0)
            {
                int randomnum = UnityEngine.Random.Range(0, 100);
                int confignum = Mathf.Clamp(plugin.mtf_and_ci_change_spawnpoint, 0, 100);

                if(confignum > randomnum && !plugin.Server.Map.WarheadDetonated)
                {
                    DecontaminationLCZ dlcz = GameObject.Find("Host").GetComponent<DecontaminationLCZ>();
                    int curAnm = dlcz.GetCurAnnouncement();
                    List<Vector3> list = new List<Vector3>();
                    Vector3 spawnpos;
                    Vector pos939 = plugin.Server.Map.GetRandomSpawnPoint(Role.SCP_939_53);
                    Vector pos049 = plugin.Server.Map.GetRandomSpawnPoint(Role.SCP_049);
                    Vector posLCZArm = SanyaPlugin.GetCameraPosByName("ARMORY") - new Vector(0, 2, 0);

                    plugin.Debug($"[RandomSpawner] Found 939");
                    list.Add(new Vector3(pos939.x, pos939.y, pos939.z));
                    plugin.Debug($"[RandomSpawner] Found 049");
                    list.Add(new Vector3(pos049.x, pos049.y, pos049.z));

                    if(!plugin.Server.Map.LCZDecontaminated && curAnm < 4)
                    {
                        plugin.Debug($"[RandomSpawner] Found LCZArm");
                        list.Add(new Vector3(posLCZArm.x, posLCZArm.y, posLCZArm.z));

                        RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                        foreach(var itempos in rnde.posIds)
                        {
                            if(itempos.posID == "RandomPistol" && itempos.position.position.y > 0.5f && itempos.position.position.y < 0.7f)
                            {
                                plugin.Debug($"[RandomSpawner] Found 372");
                                list.Add(new Vector3(itempos.position.position.x, itempos.position.position.y, itempos.position.position.z));
                            }
                            else if(itempos.posID == "toilet_keycard" && itempos.position.position.y > 1.25f && itempos.position.position.y < 1.35f)
                            {
                                plugin.Debug($"[RandomSpawner] Found Toilet");
                                list.Add(new Vector3(itempos.position.position.x, itempos.position.position.y - 0.5f, itempos.position.position.z));
                            }
                        }
                    }

                    foreach(GameObject roomid in GameObject.FindGameObjectsWithTag("RoomID"))
                    {
                        Rid rid = roomid.GetComponent<Rid>();
                        if(rid != null && (rid.id == "LC_ARMORY" || rid.id == "Shelter"))
                        {
                            plugin.Debug($"[RandomSpawner] Found {rid.id}");
                            list.Add(roomid.transform.position);
                        }
                    }

                    plugin.Info($"[RandomSpawner] SpawnList:{list.Count}/LCZDecont:{plugin.Server.Map.LCZDecontaminated}/curAnm:{curAnm} [{confignum}>{randomnum}]");
                    spawnpos = list[UnityEngine.Random.Range(0, list.Count)];

                    foreach(var ply in ev.PlayerList)
                    {
                        Timing.RunCoroutine(SanyaPlugin._DelayedTeleport(ply, new Vector(spawnpos.x, spawnpos.y + 1, spawnpos.z), false));
                    }
                }
                else
                {
                    plugin.Info($"[RandomSpawner] Passed... [{confignum}>{randomnum}]/Detonated:{plugin.Server.Map.WarheadDetonated}");
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

            //さにゃぱっち
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

                                player.ThrowGrenade(GrenadeType.FRAG_GRENADE, true, new Vector(0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(GrenadeType.FRAG_GRENADE, true, new Vector(0, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(GrenadeType.FRAG_GRENADE, true, new Vector(-0.25f, 1, 0), true, newpos, true, 1.0f);

                                player.Kill(DamageType.RAGDOLLLESS);
                            }
                            else if(ev.KnobSetting == KnobSetting.ONE_TO_ONE && !isAfterChange && player.TeamRole.Role != Role.SCP_049_2)
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

                                player.ThrowGrenade(GrenadeType.FLASHBANG, true, new Vector(0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(GrenadeType.FLASHBANG, true, new Vector(0, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(GrenadeType.FLASHBANG, true, new Vector(-0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.Kill(DamageType.RAGDOLLLESS);

                                plugin.Server.Map.SpawnItem((ItemType)rnd.Next(0, 30), newpos, new Vector(0, 0, 0));
                            }
                            else if(ev.KnobSetting == KnobSetting.VERY_FINE)
                            {
                                plugin.Info($"[SCP914] VERY_FINE:{player.Name}");

                                player.ThrowGrenade(GrenadeType.FLASHBANG, true, new Vector(0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(GrenadeType.FLASHBANG, true, new Vector(0, 1, 0), true, newpos, true, 1.0f);
                                player.ThrowGrenade(GrenadeType.FLASHBANG, true, new Vector(-0.25f, 1, 0), true, newpos, true, 1.0f);
                                player.ChangeRole(Role.SCP_049_2, true, false, true, false);

                                dot_target.Add(player);
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
                ev.AllowSummon = false;
            }
        }

        public void OnIntercom(PlayerIntercomEvent ev)
        {
            plugin.Info($"[OnIntercom] {ev.Player.Name} : {ev.SpeechTime} : {ev.CooldownTime}");

            //Timing.RunCoroutine(SanyaPlugin.DeductBatteryHasTransmission(ev.Player), Segment.Update);
        }

        public void OnGeneratorAccess(PlayerGeneratorAccessEvent ev)
        {
            plugin.Debug($"[OnGeneratorAccess] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}/{ev.Generator.StartTime}:{ev.Generator.Engaged}");

            if(plugin.generator_engaged_cantopen)
            {
                if(ev.Generator.Engaged)
                {
                    ev.Allow = false;
                }
            }

            if(isAlreadyLoneDeath)
            {
                plugin.Debug($"[079lonedeath_locked] {ev.Generator.Room.RoomType}");
                ev.Allow = false;
            }
        }

        public void OnGeneratorUnlock(PlayerGeneratorUnlockEvent ev)
        {
            plugin.Debug($"[OnGeneratorUnlock] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}/{ev.Generator.StartTime}:{ev.Generator.Engaged}");
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

            if(plugin.scp079_lone_boost)
            {
                bool iswithoutscp079 = false;
                foreach(Smod2.API.Player player in plugin.Server.GetPlayers())
                {
                    if(player.TeamRole.Team == Smod2.API.Team.SCP && player.TeamRole.Role != Role.SCP_079)
                    {
                        iswithoutscp079 = true;
                    }
                }

                if(!iswithoutscp079)
                {
                    plugin.Debug($"[079loneUnlock] {ev.Generator.Room.RoomType}");
                    ev.Allow = true;
                }
            }

            if(isAlreadyLoneDeath)
            {
                plugin.Debug($"[079lonedeath_locked] {ev.Generator.Room.RoomType}");
                ev.Allow = false;
            }

            if(ev.Allow)
            {
                ev.Generator.Open = true;
            }
        }

        public void OnGeneratorInsertTablet(PlayerGeneratorInsertTabletEvent ev)
        {
            plugin.Debug($"[OnGeneratorInsertTablet] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}({ev.RemoveTablet})");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}/{ev.Generator.StartTime}:{ev.Generator.Engaged}");

            if(ev.Player.GetBypassMode())
            {
                ev.Allow = true;
            }

            if(plugin.cassie_subtitle)
            {
                if(ev.Allow)
                {
                    string[] genname = plugin.TranslateGeneratorName(ev.Generator.Room.RoomType);

                    if(roundduring)
                    {
                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast(10, plugin.generator_starting_message.Replace("[genname]", genname[0]).Replace("[genname_en]", genname[1]), false);
                    }
                }
            }
        }

        public void OnGeneratorEjectTablet(PlayerGeneratorEjectTabletEvent ev)
        {
            plugin.Debug($"[OnGeneratorEjectTablet] {ev.Player.Name}:{ev.Generator.Room.RoomType}:{ev.Allow}({ev.SpawnTablet})");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}/{ev.Generator.StartTime}:{ev.Generator.Engaged}");

            if(ev.Player.GetBypassMode())
            {
                ev.Allow = true;
            }
        }

        public void OnGeneratorFinish(GeneratorFinishEvent ev)
        {
            plugin.Debug($"[OnGeneratorFinish] {ev.Generator.Room.RoomType}");
            plugin.Debug($"{ev.Generator.Locked}:{ev.Generator.Open}:{ev.Generator.HasTablet}:{ev.Generator.TimeLeft}/{ev.Generator.StartTime}:{ev.Generator.Engaged}");

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
                string[] genname = plugin.TranslateGeneratorName(ev.Generator.Room.RoomType);

                if(roundduring)
                {
                    if(!gencomplete)
                    {
                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast(10, plugin.generator_complete_message.Replace("[genname]", genname[0]).Replace("[genname_en]", genname[1]).Replace("[cur]", engcount.ToString()).Replace("[max]", "5"), false);
                    }
                    else
                    {
                        plugin.Server.Map.ClearBroadcasts();
                        plugin.Server.Map.Broadcast(10, plugin.generator_readyforall_message.Replace("[genname]", genname[0]).Replace("[genname_en]", genname[1]).Replace("[cur]", engcount.ToString()).Replace("[max]", "5"), false);
                    }
                }
            }
        }

        public void On079AddExp(Player079AddExpEvent ev)
        {
            plugin.Debug($"[On079AddExp] {ev.Player.Name}:{ev.ExpToAdd}:{ev.ExperienceType.ToString()}");

            if(plugin.level_enabled && plugin.data_enabled)
            {
                switch(ev.ExperienceType)
                {
                    case ExperienceType.KILL_ASSIST_CLASSD:
                    case ExperienceType.KILL_ASSIST_CHAOS_INSURGENCY:
                    case ExperienceType.KILL_ASSIST_NINETAILFOX:
                    case ExperienceType.KILL_ASSIST_SCIENTIST:
                    case ExperienceType.KILL_ASSIST_SCP:
                    case ExperienceType.KILL_ASSIST_OTHER:
                        PlayerData curData = plugin.playersData.Find(x => x.steamid == ev.Player.SteamId);
                        if(curData != null)
                        {
                            plugin.Debug($"[ExpAdd/Assist] {curData.exp}+={plugin.level_exp_kill} / {curData.steamid}:{ev.Player.Name}");
                            ev.Player.SendConsoleMessage($"[AddExp] +{plugin.level_exp_kill}(Next:{Mathf.Clamp(curData.level * 3 - curData.exp + plugin.level_exp_kill, 0, curData.level * 3 - curData.exp + plugin.level_exp_kill)})");
                            curData.AddExp(plugin.level_exp_kill, ev.Player);
                        }
                        else
                        {
                            plugin.Error($"[ExpAdd/Assist] Missing Data,Passed...[{ev.Player.SteamId}]");
                        }
                        break;
                }
            }
        }

        public void On079Door(Player079DoorEvent ev)
        {
            plugin.Debug($"[On079Door] {ev.Player.Name} (Tier:{ev.Player.Scp079Data.Level + 1}:{ev.Door.Name}):{ev.Allow}");

            if(ev.Allow && !(ev.Door.GetComponent() as Door).moving.moving)
            {
                SanyaPlugin.CallAmbientSound(rnd.Next(0, 32));
            }
        }

        public void On079Elevator(Player079ElevatorEvent ev)
        {
            plugin.Debug($"[On079Elevator] {ev.Player.Name} (Tier:{ev.Player.Scp079Data.Level + 1}:{ev.Elevator.ElevatorType}):{ev.Allow}");

            if(ev.Allow && ev.Elevator.ElevatorStatus != ElevatorStatus.Moving && (ev.Elevator.GetComponent() as Lift).operative)
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

            if(plugin.scp079_all_flick_light_tier > 0 && ev.Player.Scp079Data.Level + 1 >= plugin.scp079_all_flick_light_tier)
            {
                plugin.Debug("079LockDown-HCZBlackout");
                Generator079.mainGenerator.CallRpcOvercharge();
                if(!plugin.Server.Map.LCZDecontaminated)
                {
                    plugin.Debug("079LockDown-LCZBlackout");
                    foreach(var room in plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA))
                    {
                        if(room.ZoneType == ZoneType.LCZ)
                        {
                            room.FlickerLights();
                        }
                    }
                }
            }
        }

        public void On079StartSpeaker(Player079StartSpeakerEvent ev)
        {
            plugin.Debug($"[On079StartSpeaker] {ev.Player.Name}:{ev.Room.RoomType}({ev.APDrain}):{ev.Allow}");
            if(plugin.scp079_speaker_no_ap_use)
            {
                ev.APDrain = 0.0f;
                ev.Player.Scp079Data.SpeakerAPPerSecond = 0.0f;
            }
        }

        public void OnScp096Enrage(Scp096EnrageEvent ev)
        {
            plugin.Debug($"[OnScp096Enrage] {ev.Player.Name}:{ev.Allow}");

            if(plugin.scp096_enraged_increase_rage > 0)
            {
                Timing.RunCoroutine(SanyaPlugin._096EnragingIncrase(), Segment.FixedUpdate);
            }
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

            if(plugin.scp106_portal_trap_human || plugin.scp106_portal_warp_scp)
            {
                if(portal != null)
                {
                    foreach(var ply in plugin.Server.GetPlayers().FindAll(x => x.TeamRole.Role != Role.SCP_106))
                    {
                        if((plugin.scp106_portal_trap_human && ply.TeamRole.Team != Smod2.API.Team.SCP)
                            || (plugin.scp106_portal_warp_scp && ply.TeamRole.Team == Smod2.API.Team.SCP)
                           )
                        {
                            Vector3 myPos = new Vector3(ply.GetPosition().x, ply.GetPosition().y, ply.GetPosition().z);
                            if(Vector3.Distance(myPos, portal.transform.position) < 2.5f && !(ply.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>().goingViaThePortal)
                            {
                                plugin.Info($"[PortalTrap] {ply.Name}/{ply.TeamRole.Role}");
                                Timing.RunCoroutine(SanyaPlugin._106PortalTrap(ply), Segment.Update);
                            }
                        }
                    }
                }
                else
                {
                    portal = GameObject.Find("SCP106_PORTAL");
                }
            }

            if(updatecounter % 60 == 0 && running && roundduring)
            {
                updatecounter = 0;

                if(plugin.scp049_healing_to_049_2_range > 0 && plugin.scp049_healing_to_049_2_amount > 0)
                {
                    try
                    {
                        foreach(Player scp049 in plugin.Server.GetPlayers(Role.SCP_049))
                        {
                            foreach(Player scp0492 in plugin.Server.GetPlayers(Role.SCP_049_2))
                            {
                                int curHealth = (scp0492.GetGameObject() as GameObject).GetComponent<PlayerStats>().health;
                                if(curHealth >= scp0492.TeamRole.MaxHP) continue;

                                float distance = Vector.Distance(scp049.GetPosition(), scp0492.GetPosition());
                                if(plugin.scp049_healing_to_049_2_range > distance)
                                {
                                    plugin.Debug($"[049-2nearheal] {scp049.Name} -({distance})-> {scp0492.Name}");
                                    scp0492.SetHealth(Mathf.Clamp(curHealth + plugin.scp049_healing_to_049_2_amount, 0, scp0492.TeamRole.MaxHP), DamageType.NONE);
                                }
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        plugin.Error($"[049-2nearheal] {e.Message}");
                    }
                }

                if(scp079_nukepanel_cooltime > 0)
                {
                    scp079_nukepanel_cooltime--;
                }

                if(scp079_fake_announce_cooltime > 0)
                {
                    scp079_fake_announce_cooltime--;
                }

                if(scp079_boost_cooltime > 0)
                {
                    scp079_boost_cooltime--;
                }

                if(scp079_attack_cooltime > 0)
                {
                    scp079_attack_cooltime--;
                }

                if(scp079_nuke_cooltime > 0)
                {
                    scp079_nuke_cooltime--;
                }

                if(eventmode == SANYA_GAME_MODE.STORY_173 || eventmode == SANYA_GAME_MODE.STORY_049 || eventmode == SANYA_GAME_MODE.HCZ)
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
                            foreach(var room in plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA))
                            {
                                if(room.ZoneType == ZoneType.LCZ)
                                {
                                    room.FlickerLights();
                                }
                            }
                            flickcount_lcz = 0;
                        }
                        else
                        {
                            flickcount_lcz++;
                        }
                    }
                }

                if(plugin.outsidezone_termination_time > 0 && plugin.Round.Duration >= plugin.outsidezone_termination_time && !airbomb_used)
                {
                    Timing.RunCoroutine(SanyaPlugin._AirSupportBomb(5, 10, true, true, true), Segment.Update);
                    airbomb_used = true;
                }

                if(plugin.intercom_information)
                {
                    GameObject host = UnityEngine.GameObject.Find("Host");
                    bool nextIsCI = host.GetComponent<MTFRespawn>().nextWaveIsCI;
                    int nextRespawn = (int)Math.Truncate(host.GetComponent<MTFRespawn>().timeToNextRespawn + host.GetComponent<MTFRespawn>().respawnCooldown);
                    int leftAutoWarhead = (int)Math.Truncate(AlphaWarheadController.host.smtimeToScheduledDetonation);
                    int leftDetonation = 0;
                    int leftDecontamination = (int)((Math.Truncate(((11.74f * 60) * 100f)) / 100f) - (Math.Truncate(host.GetComponent<DecontaminationLCZ>().time * 100f) / 100f));
                    int leftgenerators = Generator079.mainGenerator.totalVoltage;

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
                            $"GENERATORS ACT : {leftgenerators.ToString("00")}/05\n",
                            $"DECONT LEFT : {(leftDecontamination / 60).ToString("00")}:{(leftDecontamination % 60).ToString("00")}\n",
                            $"DETONATE LEFT : {(leftDetonation / 60).ToString("00")}:{(leftDetonation % 60).ToString("00")}\n",
                            $"AUTO WARHEAD LEFT : {(leftAutoWarhead / 60).ToString("00")}:{(leftAutoWarhead % 60).ToString("00")}\n",
                            $"NEXT SUPPORT ENTER : {(nextRespawn / 60).ToString("00")}:{(nextRespawn % 60).ToString("00")}\n"
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

                if(!Timing.IsRunning(infosender) && !disableSender)
                {
                    infosender = Timing.RunCoroutine(this.Sender(), Segment.Update, "InfoSender");
                }
            }
            updatecounter++;
        }

        public void OnCallCommand(PlayerCallCommandEvent ev)
        {
            plugin.Debug($"[OnCallCommand] [Before] Called:{ev.Player.Name} Command:{ev.Command} Return:{ev.ReturnMessage}");

            bool isBypass = ev.Player.GetBypassMode();
            if(plugin.user_command_enabled)
            {
                int cooltime = -1;
                if(playeridlist.TryGetValue(ev.Player.PlayerId, out cooltime))
                {
                    if(cooltime == 0 || isBypass)
                    {
                        playeridlist[ev.Player.PlayerId] = plugin.user_command_cooltime;
                    }
                    else
                    {
                        plugin.Debug($"[OnCallCommand] Command Rejected:{ev.Player.Name} CT:{cooltime}");
                        ev.ReturnMessage = plugin.user_command_rejected_toofast;
                        return;
                    }
                }
                else
                {
                    playeridlist.Add(ev.Player.PlayerId, plugin.user_command_cooltime);
                }

                if(ev.ReturnMessage == "Command not found.")
                {
                    if(ev.Command.StartsWith("kill") && (plugin.user_command_enabled_kill || isBypass))
                    {
                        if(roundduring)
                        {
                            if(ev.Player.TeamRole.Role == Role.SCP_079)
                            {
                                bool iswithoutscp079 = false;
                                foreach(var player in plugin.Server.GetPlayers())
                                {
                                    if(player.TeamRole.Team == Smod2.API.Team.SCP && player.TeamRole.Role != Role.SCP_079 && player.TeamRole.Role != Role.SCP_049_2 && player.PlayerId != ev.Player.PlayerId)
                                    {
                                        iswithoutscp079 = true;
                                    }
                                }

                                if(!iswithoutscp079)
                                {
                                    ev.ReturnMessage = "Success.";
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, plugin.user_command_kill_success, false);
                                    ev.Player.SetGodmode(false);
                                    ev.Player.Kill(DamageType.TESLA);
                                    plugin.Server.Map.AnnounceCustomMessage("SCP 0 7 9 SUCCESSFULLY TERMINATED BY AUTOMATIC SECURITY SYSTEM");
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_not_round;
                                }
                            }
                            else if(ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR && ev.Player.TeamRole.Team != Smod2.API.Team.NONE && ev.Player.TeamRole.Team != Smod2.API.Team.SCP || ev.Player.GetBypassMode())
                            {
                                plugin.Debug($"[Suicide] {ev.Player.Name}");
                                if(!plugin.suicide_need_weapon || ev.Player.GetBypassMode())
                                {
                                    ev.ReturnMessage = "Success.";
                                    ev.Player.PersonalClearBroadcasts();
                                    ev.Player.PersonalBroadcast(3, plugin.user_command_kill_success, false);
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
                                                ev.Player.PersonalBroadcast(3, plugin.user_command_kill_success, false);
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
                                                ev.Player.PersonalBroadcast(3, plugin.user_command_kill_success, false);
                                                ev.Player.SetGodmode(false);
                                                Timing.RunCoroutine(SanyaPlugin._DelayedSuicideWithWeaponSound(ev.Player, DamageType.COM15), Segment.Update);
                                                break;
                                            case ItemType.E11_STANDARD_RIFLE:
                                                ev.ReturnMessage = "Success.";
                                                ev.Player.PersonalClearBroadcasts();
                                                ev.Player.PersonalBroadcast(3, plugin.user_command_kill_success, false);
                                                ev.Player.SetGodmode(false);
                                                Timing.RunCoroutine(SanyaPlugin._DelayedSuicideWithWeaponSound(ev.Player, DamageType.E11_STANDARD_RIFLE), Segment.Update);
                                                break;
                                            case ItemType.LOGICER:
                                            case ItemType.MP4:
                                                ev.ReturnMessage = "Success.";
                                                ev.Player.PersonalClearBroadcasts();
                                                ev.Player.PersonalBroadcast(3, plugin.user_command_kill_success, false);
                                                ev.Player.SetGodmode(false);
                                                Timing.RunCoroutine(SanyaPlugin._DelayedSuicideWithWeaponSound(ev.Player, DamageType.LOGICER), Segment.Update);
                                                break;
                                            default:
                                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        ev.ReturnMessage = "Success.";
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(3, plugin.user_command_kill_success, false);
                                        ev.Player.SetGodmode(false);
                                        dot_target.Add(ev.Player);
                                        Timing.RunCoroutine(SanyaPlugin._DOTDamage(ev.Player, 10, 1, 999, DamageType.DECONT), Segment.Update);
                                    }
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                            }
                        }

                    }
                    else if(ev.Command.StartsWith("sinfo") && (plugin.user_command_enabled_sinfo || isBypass))
                    {
                        if(roundduring)
                        {
                            if(ev.Player.TeamRole.Team == Smod2.API.Team.SCP || ev.Player.GetBypassMode())
                            {
                                plugin.Debug($"[SCPInfo] {ev.Player.Name}");

                                List<Player> scps = plugin.Server.GetPlayers().FindAll(fl => { return fl.TeamRole.Team == Smod2.API.Team.SCP; });
                                int scp049_2_count = scps.FindAll(x => x.TeamRole.Role == Role.SCP_049_2).Count;
                                string scplist = $"- <color=#00ff00>S A N Y A P L U G I N</color> <color=#0000ff>#</color> <color=#ff0000>S C P I N F O</color> <color=#0000ff>#</color> <color=#00ff00>I N T E R F A C E</color> -\n";
                                scplist += $"{{ <color=#ff0000>SCP陣営(049-2):</color>{scps.Count}({scp049_2_count}) / <color=#00ffff>MTF</color><color=#ffff00>陣営:</color>{plugin.Round.Stats.ScientistsAlive + plugin.Round.Stats.NTFAlive} / <color=#00ff00>CI</color><color=#ff7f00>陣営:</color>{plugin.Round.Stats.ClassDAlive + plugin.Round.Stats.CiAlive} }}\n";
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
                                        scplist += $"{items.Name} : {items.TeamRole.Name} : Tier{items.Scp079Data.Level + 1}/{(int)Math.Truncate(items.Scp079Data.AP)}AP : {zonename} : {ply079.currentCamera.cameraName}\n";
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
                                        scplist += $"{items.Name} : {items.TeamRole.Name} : {items.GetHealth()}HP : {zonename}\n";
                                    }
                                }

                                ev.ReturnMessage = scplist;
                                ev.Player.PersonalClearBroadcasts();
                                ev.Player.PersonalBroadcast(10, $"<size=25>{scplist}</size>", false);
                            }
                            else if(ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR && ev.Player.TeamRole.Team != Smod2.API.Team.NONE)
                            {
                                if(ev.Player.GetCurrentItem() != null && ev.Player.GetCurrentItem().ItemType == ItemType.MICROHID)
                                {
                                    Inventory inv = ev.Player.GetCurrentItem().GetComponent() as Inventory;
                                    bool isReady = false;

                                    if(inv.GetItemIndex() >= 0 && inv.items[inv.GetItemIndex()].durability > 0)
                                    {
                                        isReady = true;
                                    }

                                    if(isReady)
                                    {

                                        ev.ReturnMessage = "MicroHID : READY";
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(10, $"<size=25>《手持ちのMicroHIDの残エネルギーは十分に充填されています。》\n </size>", false);
                                    }
                                    else
                                    {

                                        ev.ReturnMessage = "MicroHID : NoAmmo";
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(10, $"<size=25>《手持ちのMicroHIDの残エネルギーは空です。》\n </size>", false);
                                    }
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = plugin.user_command_rejected_not_round;
                        }
                    }
                    else if(ev.Command.StartsWith("939sp") && (plugin.user_command_enabled_939sp || isBypass))
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
                                    ev.Player.PersonalBroadcast(3, plugin.user_command_broadcast_start, false);
                                    ev.ReturnMessage = "Success.(Start)";
                                }
                                else
                                {
                                    if(ev.Player.Name == plugin.Server.Map.GetIntercomSpeaker().Name)
                                    {
                                        plugin.Server.Map.SetIntercomSpeaker(null);
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(3, plugin.user_command_broadcast_stop, false);
                                        ev.ReturnMessage = "Sucess.(End)";
                                    }
                                    else
                                    {
                                        ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                    }
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = plugin.user_command_rejected_not_round;
                        }
                    }
                    else if(ev.Command.StartsWith("079nuke") && (plugin.user_command_enabled_079nuke || isBypass))
                    {
                        if(roundduring)
                        {
                            if(ev.Player.TeamRole.Role == Role.SCP_079)
                            {
                                plugin.Info($"[079nuke] {ev.Player.Name} [Tier:{ev.Player.Scp079Data.Level + 1} AP:{ev.Player.Scp079Data.AP}]");
                                if(ev.Player.Scp079Data.Level >= 2 && ev.Player.Scp079Data.AP >= 125 && scp079_nuke_cooltime <= 0)
                                {
                                    if(!AlphaWarheadController.host.inProgress)
                                    {
                                        AlphaWarheadController.host.InstantPrepare();
                                        AlphaWarheadController.host.StartDetonation(ev.Player.GetGameObject() as UnityEngine.GameObject);
                                        ev.ReturnMessage = "Success.(Start:AP-125)";
                                    }
                                    else
                                    {
                                        AlphaWarheadController.host.CancelDetonation(ev.Player.GetGameObject() as UnityEngine.GameObject);
                                        ev.ReturnMessage = "Success.(Stop:AP-125)";
                                    }

                                    if(!ev.Player.GetBypassMode())
                                    {
                                        ev.Player.Scp079Data.AP -= 125.0f;
                                    }
                                    scp079_nuke_cooltime = 10;
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = plugin.user_command_rejected_not_round;
                        }
                    }
                    else if(ev.Command.StartsWith("079sp") && (plugin.user_command_enabled_079sp || isBypass))
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
                                    ev.ReturnMessage = "Success.(AP-5)";
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = plugin.user_command_rejected_not_round;
                        }
                    }
                    else if(ev.Command.StartsWith("radio") && (plugin.user_command_enabled_radio || isBypass))
                    {
                        if(roundduring)
                        {
                            Radio radio = (ev.Player.GetGameObject() as GameObject).GetComponent<Radio>();
                            if(radio != null
                                && ev.Player.TeamRole.Team != Smod2.API.Team.SCP
                                && ev.Player.TeamRole.Team != Smod2.API.Team.SPECTATOR
                                )
                            {
                                bool isIcom = false;
                                if(ev.Player.GetCurrentItemIndex() > 0 && plugin.user_command_enabled_radio_intercom)
                                {
                                    if(ev.Player.GetCurrentItem().ItemType == ItemType.RADIO)
                                    {
                                        isIcom = true;
                                        Inventory inv = (ev.Player.GetGameObject() as GameObject).GetComponent<Inventory>();

                                        if(ev.Player.GetPosition().y < -1900 && ev.Player.GetPosition().y > -2000)
                                        {
                                            ev.ReturnMessage = "Failed.(no signal on pocket dimension.)";
                                        }
                                        else if(Intercom.host.remainingCooldown > 0f)
                                        {
                                            ev.ReturnMessage = "Failed.(Intercom has cooldown)";
                                        }
                                        else if(!Intercom.host.speaking)
                                        {
                                            if(plugin.Server.Map.GetIntercomSpeaker() == null)
                                            {
                                                if(inv.items[ev.Player.GetItemIndex(ItemType.RADIO)].durability > 0)
                                                {
                                                    ev.Player.PersonalClearBroadcasts();
                                                    ev.Player.PersonalBroadcast(3, plugin.user_command_broadcast_start, false);
                                                    ev.ReturnMessage = "Success.(Start)";
                                                    plugin.Server.Map.SetIntercomSpeaker(ev.Player);
                                                }
                                                else
                                                {
                                                    ev.ReturnMessage = "Failed.(No battery)";
                                                }
                                            }
                                            else
                                            {
                                                plugin.Error("!speaking && GetIntercomSpeaker() != null");
                                                ev.ReturnMessage = "Error(GetIntercomSpeaker!=null)";
                                            }
                                        }
                                        else
                                        {
                                            plugin.Debug("Radio now check");
                                            if(plugin.Server.Map.GetIntercomSpeaker() != null)
                                            {
                                                plugin.Debug($"radio_now_id & {plugin.Server.Map.GetIntercomSpeaker().PlayerId} : {ev.Player.PlayerId}");
                                                if(ev.Player.PlayerId == plugin.Server.Map.GetIntercomSpeaker().PlayerId)
                                                {
                                                    ev.Player.PersonalClearBroadcasts();
                                                    ev.Player.PersonalBroadcast(3, plugin.user_command_broadcast_stop, false);
                                                    ev.ReturnMessage = "Success.(Stop)";
                                                    plugin.Server.Map.SetIntercomSpeaker(null);
                                                }
                                                else
                                                {
                                                    ev.ReturnMessage = "Failed.(Other broadcasting now)";
                                                }
                                            }
                                            else
                                            {
                                                ev.ReturnMessage = "Failed.(Intercom not ready)";
                                            }
                                        }
                                    }
                                }

                                if(ev.Player.GetInventory().FindIndex(x => x.ItemType == ItemType.RADIO) != -1 && (!isIcom || !plugin.user_command_enabled_radio_intercom))
                                {
                                    if(ev.Player.RadioStatus != RadioStatus.CLOSE)
                                    {
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(3, plugin.user_command_radio_off, false);
                                        radio.CallCmdUpdatePreset((int)RadioStatus.CLOSE);
                                    }
                                    else
                                    {
                                        ev.Player.PersonalClearBroadcasts();
                                        ev.Player.PersonalBroadcast(3, plugin.user_command_radio_on, false);
                                        radio.CallCmdUpdatePreset((int)RadioStatus.SHORT_RANGE);
                                    }
                                    ev.ReturnMessage = $"Radio Status:{ev.Player.RadioStatus}";
                                }
                                else if(!isIcom)
                                {
                                    ev.ReturnMessage = "Failed.(doesnt have radio)";
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                            }
                        }
                        else
                        {
                            ev.ReturnMessage = plugin.user_command_rejected_not_round;
                        }
                    }
                    else if(ev.Command.StartsWith("boost") && (plugin.user_command_enabled_boost || isBypass))
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
                                        ev.Player.PersonalBroadcast(3, plugin.user_command_boost_success.Replace("[role]", ev.Player.TeamRole.Name).Replace("[hp]", $"-{usehp}"), false);
                                        if(!ev.Player.GetBypassMode())
                                        {
                                            ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                        }
                                        ply096.rageProgress = 1f;
                                        ev.ReturnMessage = $"Boost!(-{usehp})";
                                    }
                                    else
                                    {
                                        ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                    }
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                }
                            }
                            else if(ev.Player.TeamRole.Role == Role.SCP_939_53 || ev.Player.TeamRole.Role == Role.SCP_939_89)
                            {
                                Scp939PlayerScript ply939 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp939PlayerScript>();
                                int health = ev.Player.GetHealth();
                                int maxhp = ev.Player.TeamRole.MaxHP;
                                int recovhp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.05);

                                if(health < maxhp && ply939 != null)
                                {
                                    GameObject gameObject = ev.Player.GetGameObject() as GameObject;
                                    Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                                    Vector3 forward = scp049sc.plyCam.transform.forward;
                                    Vector3 position = scp049sc.plyCam.transform.position;

                                    RaycastHit raycastHit;
                                    if(Physics.Raycast(position, forward, out raycastHit, scp049sc.recallDistance, SanyaPlugin.ragdollmask))
                                    {
                                        Ragdoll ragdoll = raycastHit.transform.GetComponentInParent<Ragdoll>();
                                        if(ragdoll != null)
                                        {
                                            plugin.Debug($"{(Role)ragdoll.owner.charclass}");

                                            if(ragdoll.owner.charclass != (int)Role.SCP_049
                                                && ragdoll.owner.charclass != (int)Role.SCP_049_2
                                                && ragdoll.owner.charclass != (int)Role.SCP_096
                                                && ragdoll.owner.charclass != (int)Role.SCP_106
                                                && ragdoll.owner.charclass != (int)Role.SCP_173
                                                && ragdoll.owner.charclass != (int)Role.SCP_939_53
                                                && ragdoll.owner.charclass != (int)Role.SCP_939_89
                                               )
                                            {
                                                if(ragdoll.gameObject.CompareTag("Ragdoll"))
                                                {
                                                    UnityEngine.Networking.NetworkServer.Destroy(ragdoll.gameObject);
                                                    ev.Player.SetHealth(Mathf.Clamp(health + recovhp, 0, maxhp), DamageType.NONE);
                                                    ev.Player.PersonalClearBroadcasts();
                                                    ev.Player.PersonalBroadcast(3, plugin.user_command_boost_success.Replace("[role]", ev.Player.TeamRole.Name).Replace("[hp]", $"+{recovhp}"), false);
                                                    ply939.CallRpcShoot();
                                                    SanyaPlugin.Call939CanSee();
                                                }
                                            }
                                        }
                                        ev.ReturnMessage = $"Boost!(+{recovhp}HP)";
                                    }
                                    else
                                    {
                                        ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                    }
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                }
                            }
                            else if(ev.Player.TeamRole.Role == Role.SCP_106)
                            {
                                if(!UnityEngine.GameObject.FindObjectOfType<OneOhSixContainer>().used)
                                {
                                    if(!(ev.Player.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>().goingViaThePortal)
                                    {

                                        if((ev.Player.GetGameObject() as GameObject).GetComponent<FallDamage>().isGrounded)
                                        {
                                            Scp106PlayerScript ply106 = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp106PlayerScript>();
                                            int health = ev.Player.GetHealth();
                                            int usehp = (int)Math.Floor(ev.Player.TeamRole.MaxHP * 0.2);
                                            List<Player> humanlist = new List<Player>();
                                            bool isTarget = false;

                                            Vector3 forward = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp049PlayerScript>().plyCam.transform.forward;
                                            Vector3 position = (ev.Player.GetGameObject() as GameObject).GetComponent<Scp049PlayerScript>().plyCam.transform.position;
                                            forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

                                            RaycastHit raycastHit;
                                            if(Physics.Raycast(position + forward, forward, out raycastHit, 500f, SanyaPlugin.playermask))
                                            {
                                                plugin.Debug($"Hit");
                                                ServerMod2.API.SmodPlayer target = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                                CharacterClassManager comp = raycastHit.transform.GetComponent<CharacterClassManager>();
                                                if(target != null && comp != null && target.PlayerId != ev.Player.PlayerId && !target.GetGodmode() && target.TeamRole.Team != Smod2.API.Team.SCP && (target.GetGameObject() as GameObject).GetComponent<FallDamage>().isGrounded)
                                                {
                                                    plugin.Debug($"[106boost_target] [{target.PlayerId}]{target.Name}");
                                                    humanlist.Add(target);
                                                    isTarget = true;
                                                }
                                            }

                                            if(humanlist.Count == 0)
                                            {
                                                plugin.Debug($"RandomTarget");
                                                foreach(Player item in plugin.Server.GetPlayers())
                                                {
                                                    if(item.TeamRole.Team != Smod2.API.Team.SCP &&
                                                        item.TeamRole.Team != Smod2.API.Team.SPECTATOR &&
                                                        item.TeamRole.Team != Smod2.API.Team.NONE &&
                                                        !item.GetGodmode() &&
                                                        (item.GetGameObject() as GameObject).GetComponent<FallDamage>().isGrounded)
                                                    {
                                                        if(!(item.GetPosition().y < -1900 && item.GetPosition().y > -2000))
                                                        {
                                                            humanlist.Add(item);
                                                        }
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
                                                        ev.Player.PersonalBroadcast(3, plugin.user_command_boost_success.Replace("[role]", ev.Player.TeamRole.Name).Replace("[hp]", $"-{usehp}"), false);
                                                        ev.ReturnMessage = $"Success.(Warp to [{humanlist[rndres].Name}].(-{usehp})";
                                                        if(!ev.Player.GetBypassMode())
                                                        {
                                                            ev.Player.SetHealth(health - usehp, DamageType.NONE);
                                                        }
                                                        if(isTarget)
                                                        {
                                                            SanyaPlugin.ShowHitmarker(ev.Player);
                                                        }
                                                        Timing.RunCoroutine(SanyaPlugin._106CreatePortalEX(ev.Player, humanlist[rndres]), Segment.Update);
                                                    }
                                                    else
                                                    {
                                                        ev.ReturnMessage = "No target";
                                                    }
                                                }
                                                else
                                                {
                                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                                }
                                            }
                                            else
                                            {
                                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                            }
                                        }
                                        else
                                        {
                                            ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                        }
                                    }
                                    else
                                    {
                                        ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                    }
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
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

                                        if(i.owner.charclass != (int)Role.SCP_049
                                            && i.owner.charclass != (int)Role.SCP_049_2
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
                                    ev.Player.PersonalBroadcast(3, plugin.user_command_boost_success.Replace("[role]", ev.Player.TeamRole.Name).Replace("[hp]", $"-{usehp}"), false);
                                    ev.ReturnMessage = $"Boost!(-{usehp})";
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
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
                                            SanyaPlugin.Call173SnapSound(ev.Player);
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
                                    ev.Player.PersonalBroadcast(3, plugin.user_command_boost_success.Replace("[role]", ev.Player.TeamRole.Name).Replace("[hp]", $"-{usehp}"), false);
                                    ev.ReturnMessage = $"Boost!(-{usehp})";
                                }
                                else
                                {
                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
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

                                                ev.ReturnMessage = "Success.(fake announce)";
                                            }
                                            else
                                            {
                                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                            }
                                        }
                                        else if(camera.cameraName.Contains("SCP-079 MAIN CAM"))
                                        {
                                            if(NineTailedFoxAnnouncer.singleton.isFree)
                                            {
                                                List<Player> plys = plugin.Server.GetPlayers(Smod2.API.Team.SCP);
                                                List<Player> without079 = plys.FindAll(x => x.TeamRole.Role != Role.SCP_079);
                                                if(without079.Count > 0)
                                                {
                                                    var target = without079[rnd.Next(0, without079.Count)];
                                                    if(target.TeamRole.Role == Role.SCP_939_53 || target.TeamRole.Role == Role.SCP_939_89)
                                                    {

                                                        plugin.Server.Map.AnnounceScpKill("939", null);
                                                    }
                                                    else
                                                    {
                                                        plugin.Server.Map.AnnounceScpKill(target.TeamRole.Name.Replace("SCP-", ""), null);
                                                    }

                                                    plugin.Server.Map.ClearBroadcasts();
                                                    plugin.Server.Map.Broadcast(10, plugin.scp_containment_unknown_message.Replace("[role]", ev.Player.TeamRole.Name).Replace("[name]", target.Name), false);


                                                    if(!ev.Player.GetBypassMode())
                                                    {
                                                        ev.Player.Scp079Data.AP = Mathf.Clamp(ev.Player.Scp079Data.AP - 50f, 0, ev.Player.Scp079Data.AP - 50f);
                                                        scp079_boost_cooltime = 30;
                                                    }

                                                    ev.ReturnMessage = "Success.(fake announce)";
                                                }
                                                else
                                                {
                                                    ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
                                                }
                                            }
                                            else
                                            {
                                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
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
                                    ev.ReturnMessage = $"Failed.(CT left:{scp079_boost_cooltime}second)";
                                }
                            }
                            else
                            {
                                ev.ReturnMessage = plugin.user_command_rejected_miss_condition;
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
                            ev.ReturnMessage = plugin.user_command_rejected_not_round;
                        }
                    }
                    else if(ev.Command.StartsWith("attack") && (plugin.user_command_enabled_attack || isBypass))
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

                                        if(Physics.Raycast(position + forward, forward, out raycastHit, 500f, SanyaPlugin.playerpluselevatormask))
                                        {
                                            plugin.Debug($"Hit -> colider:{raycastHit.collider.name}/transform:{raycastHit.transform.name}/tag:{raycastHit.transform.tag}");
                                            Generator079 gen = raycastHit.collider.GetComponentInParent<Generator079>();
                                            BreakableWindow bwin = raycastHit.collider.GetComponent<BreakableWindow>();

                                            if(raycastHit.collider.name.Contains("NukeUP"))
                                            {
                                                if(Vector3.Distance(AlphaWarheadOutsitePanel.nukeside.transform.position, raycastHit.point) < 2f)
                                                {
                                                    plugin.Debug("nukepanel");
                                                    if(ev.Player.Scp079Data.Level >= 2 && scp079_nukepanel_cooltime == 0)
                                                    {
                                                        if(!AlphaWarheadController.host.inProgress)
                                                        {
                                                            AlphaWarheadController.host.InstantPrepare();
                                                            AlphaWarheadController.host.StartDetonation(gameObject);
                                                        }
                                                        else
                                                        {
                                                            AlphaWarheadController.host.CancelDetonation(gameObject);
                                                        }
                                                        scp079_nukepanel_cooltime = 45;
                                                    }
                                                }
                                            }
                                            else if(raycastHit.collider.name.Contains("Intercom_"))
                                            {
                                                if(Vector3.Distance(raycastHit.point, GameObject.Find("IntercomMonitor").transform.position) < 3.5f)
                                                {
                                                    plugin.Debug($"intercom");
                                                    if(NineTailedFoxAnnouncer.singleton.isFree && scp079_fake_announce_cooltime == 0)
                                                    {
                                                        int number;
                                                        char letter;
                                                        NineTailedFoxUnits.host.NewName(out number, out letter);
                                                        plugin.Server.Map.AnnounceNtfEntrance(plugin.Server.Round.Stats.SCPAlive, number, letter);
                                                        scp079_fake_announce_cooltime = 30;
                                                    }
                                                }
                                            }
                                            else if(raycastHit.transform.CompareTag("914_use"))
                                            {
                                                plugin.Debug($"914 use");
                                                SanyaPlugin.Call914Use(ev.Player);
                                            }
                                            else if(raycastHit.transform.CompareTag("914_knob"))
                                            {
                                                plugin.Debug($"914 knob");
                                                SanyaPlugin.Call914Change(ev.Player);
                                            }
                                            else if(gen != null)
                                            {
                                                plugin.Debug("Generator");
                                                if(!gen.isDoorOpen)
                                                {
                                                    gen.Interact(gameObject, "EPS_DOOR");
                                                }
                                                else if(gen.isTabletConnected)
                                                {
                                                    gen.Interact(gameObject, "EPS_CANCEL");
                                                }
                                                else if(!gen.isTabletConnected && gen.isDoorOpen)
                                                {
                                                    gen.Interact(gameObject, "EPS_DOOR");
                                                }
                                            }
                                            else if(bwin != null)
                                            {
                                                plugin.Debug("Window");
                                                bwin.ServerDamageWindow(5f);
                                            }
                                            else
                                            {
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
                                    ev.ReturnMessage = $"Failed.(CT left:{scp079_attack_cooltime}second)";
                                }
                            }
                            else if(ev.Player.GetCurrentItem() != null && ev.Player.GetCurrentItem().ItemType == ItemType.MEDKIT)
                            {
                                UnityEngine.GameObject gameObject = ev.Player.GetGameObject() as UnityEngine.GameObject;
                                CharacterClassManager plychm = gameObject.GetComponent<CharacterClassManager>();
                                Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                                Vector3 forward = scp049sc.plyCam.transform.forward;
                                Vector3 position = scp049sc.plyCam.transform.position;
                                if(gameObject != null && forward != null && position != null)
                                {
                                    RaycastHit raycastHit;
                                    if(Physics.Raycast(position + forward, forward, out raycastHit, scp049sc.distance, SanyaPlugin.playermask))
                                    {
                                        plugin.Debug("Hit");
                                        ServerMod2.API.SmodPlayer ply = new ServerMod2.API.SmodPlayer(raycastHit.transform.gameObject);
                                        CharacterClassManager comp = raycastHit.transform.GetComponent<CharacterClassManager>();
                                        PlayerStats stats = raycastHit.transform.GetComponent<PlayerStats>();

                                        if(comp != null && ply != null && stats != null)
                                        {
                                            if(ev.Player.PlayerId != ply.PlayerId)
                                            {
                                                plugin.Debug("Heal");
                                                if(stats.health < stats.ccm.klasy[stats.ccm.curClass].maxHP)
                                                {
                                                    int healamount = UnityEngine.Random.Range(65, 90);
                                                    stats.health = Mathf.Clamp(stats.health + healamount, 0, stats.ccm.klasy[stats.ccm.curClass].maxHP);
                                                    ev.Player.GetCurrentItem().Remove();
                                                    ev.Player.PersonalClearBroadcasts();
                                                    ev.Player.PersonalBroadcast(3, $"<size=25>《<{ply.Name}>を回復させました。》\n </size><size=20>《Healed to {ply.Name}.》\n</size>", false);
                                                    ply.PersonalClearBroadcasts();
                                                    ply.PersonalBroadcast(3, $"<size=25>《<{ev.Player.Name}>より回復を受けました。》\n </size><size=20>《Healed from {ev.Player.Name}.》\n</size>", false);
                                                }
                                                else
                                                {
                                                    ev.Player.PersonalClearBroadcasts();
                                                    ev.Player.PersonalBroadcast(3, $"<size=25>《<{ply.Name}>はHPがフルです。》\n </size><size=20>《{ply.Name} has full HP.》\n</size>", false);
                                                }
                                            }
                                            else
                                            {
                                                plugin.Debug($"SelfMed");
                                            }
                                        }
                                    }
                                }
                                ev.ReturnMessage = "Medkit for Friends.";
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
                                        SanyaPlugin.Explode(ev.Player, new Vector(raycastHit.point.x, raycastHit.point.y, raycastHit.point.z), false, true);
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
                                            else if(ev.Player.TeamRole.Team != ply.TeamRole.Team || ServerConsole.FriendlyFire)
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

                                                float damage = 1.0f;

                                                if(ev.Player.TeamRole.Role == Role.FACILITY_GUARD)
                                                {
                                                    damage = 5.0f;
                                                }

                                                gameObject.GetComponent<PlayerStats>().HurtPlayer(
                                                    new PlayerStats.HitInfo(
                                                        SanyaPlugin.GetDamageFromHitbox(hitbox, damage),
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
                            ev.ReturnMessage = plugin.user_command_rejected_not_round;
                        }
                    }
                    else if(ev.Command.StartsWith("test"))
                    {
                        plugin.Warn($"[test_user] {ev.Player.Name}");
                        GameObject host = GameObject.Find("Host");
                        GameObject gameObject = ev.Player.GetGameObject() as GameObject;
                        Inventory inv = gameObject.GetComponent<Inventory>();
                        Player hostplayer = new ServerMod2.API.SmodPlayer(host);


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
                        //Scp049PlayerScript scp049sc = gameObject.GetComponent<Scp049PlayerScript>();
                        //Vector3 forward = scp049sc.plyCam.transform.forward;
                        //Vector3 position = scp049sc.plyCam.transform.position;
                        //forward.Scale(new Vector3(0.5f, 0.5f, 0.5f));

                        //if(gameObject != null)
                        //{
                        //    RaycastHit raycastHit;
                        //    if(Physics.Raycast(position + forward, forward, out raycastHit, 500f, SanyaPlugin.playerpluselevatormask))
                        //    {
                        //        plugin.Debug($"name:{raycastHit.transform.name} layer:{raycastHit.transform.gameObject.layer}");
                        //        //SanyaPlugin.Explode(ev.Player, new Vector(raycastHit.point.x, raycastHit.point.y, raycastHit.point.z));
                        //    }
                        //}
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

                        //if(ev.Player.TeamRole.Role == Role.SCP_079)
                        //{
                        //    plugin.Error($"{ev.Player.Scp079Data.Exp}/{ev.Player.Scp079Data.ExpToLevelUp} {ev.Player.Scp079Data.Level+1}:{ev.Player.Scp079Data.AP}");
                        //}

                        //foreach(var data in plugin.playersData)
                        //{
                        //    if(ev.Player.SteamId == data.steamid)
                        //    {
                        //        data.AddExp(100,ev.Player);
                        //    }
                        //}

                        //Inventory inv = gameObject.GetComponent<Inventory>();
                        //plugin.Error($"{(ItemType)inv.items[inv.GetItemIndex()].id}:{inv.items[inv.GetItemIndex()].durability}");

                        //Scp079PlayerScript ply079 = gameObject.GetComponent<Scp079PlayerScript>();

                        //Vector3 forward = ply079.currentCamera.targetPosition.transform.forward;
                        //Vector3 position = ply079.currentCamera.transform.position;

                        //Timing.RunCoroutine(SanyaPlugin._GrenadeLauncher(ev.Player, new Vector(position.x, position.y, position.z), new Vector(forward.x, forward.y, forward.z)), Segment.Update);

                        //var lcz = host.GetComponent<DecontaminationLCZ>();
                        //lcz.CallRpcPlayAnnouncement(lcz.GetCurAnnouncement(), true);
                        //plugin.Error($"{lcz.GetCurAnnouncement()}");

                        //if(SanyaPlugin.test)
                        //{
                        //    SanyaPlugin.test = false;
                        //}
                        //else
                        //{
                        //    SanyaPlugin.test = true;
                        //}

                        //GameObject portal = GameObject.Find("SCP106_PORTAL");
                        //Vector3 myPos = new Vector3(ev.Player.GetPosition().x, ev.Player.GetPosition().y, ev.Player.GetPosition().z);

                        //if(portal != null)
                        //{
                        //    plugin.Error($"{portal.transform.position} / {Vector3.Distance(myPos, portal.transform.position)}");
                        //}
                        //else
                        //{
                        //    plugin.Error("null");
                        //}

                        //Scp106PlayerScript ply106 = gameObject.GetComponent<Scp106PlayerScript>();
                        //ply106.CallRpcTeleportAnimation();

                        //Timing.RunCoroutine(SanyaPlugin._106PortalTrap(ev.Player), Segment.Update);

                        //plugin.Debug($"{plugin.Server.Map.GetRandomSpawnPoint(Role.SCP_939_53)}");

                        //Scp079PlayerScript p079 = gameObject.GetComponent<Scp079PlayerScript>();
                        //ServerMod2.API.SmodPlayer player = new ServerMod2.API.SmodPlayer(gameObject);

                        //plugin.Error($"{p079.GetOtherRoom().currentRoom}/{p079.GetOtherRoom().currentZone}");
                        //plugin.Error($"{player.GetCurrentRoom().name}");

                        //ev.Player.ChangeRole(Role.TUTORIAL, true, false);

                        ev.ReturnMessage = "test ok(user command)";
                    }
                }
            }

            plugin.Debug("[OnCallCommand] Called:" + ev.Player.Name + " Command:" + ev.Command + " Return:" + ev.ReturnMessage);
        }

        public void OnShoot(PlayerShootEvent ev)
        {
            plugin.Debug($"[OnShoot] {ev.Player.Name}[{ev.SourcePosition}({ev.Direction})] -> {ev.Target?.Name}{ev.TargetPosition}({ev.TargetHitbox}) [{ev.Weapon}:{ev.WeaponSound}] [{ev.ShouldSpawnHitmarker}:{ev.ShouldSpawnBloodDecal}]");


            //if(ev.Weapon == DamageType.COM15)
            //{
            //    Timing.RunCoroutine(SanyaPlugin._GrenadeLauncher(ev.Player),Segment.Update);
            //}

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
