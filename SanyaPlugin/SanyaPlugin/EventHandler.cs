using System;
using System.IO;
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
    class SCPList
    {
        public SCPList(Player ply, Vector p)
        {
            player = ply;
            beforePos = p;
        }
        public Player player;
        public Vector beforePos;
    }

    class Playerinfo
    {
        public Playerinfo() { }

        public string name { get; set; }
        
        public string steamid { get; set; }

        public string role { get; set; }
    }

    class Serverinfo {
        public Serverinfo()
        {
            players = new List<Playerinfo>();
        }
        public string time { get; set; }

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
        IEventHandlerRoundStart,
        IEventHandlerRoundEnd,
        IEventHandlerLCZDecontaminate,
        IEventHandlerWarheadDetonate,
        IEventHandlerCheckEscape,
        IEventHandlerSetRole,
        IEventHandlerPlayerDie,
        IEventHandlerInfected,
        IEventHandlerDoorAccess,
        IEventHandlerRadioSwitch,
        IEventHandlerElevatorUse,
        IEventHandlerPlayerJoin,
        IEventHandlerUpdate
    {
        private Plugin plugin;
        private Thread infosender;
        private bool running = false;
        private bool sender_live = true;


        //-----------------var---------------------
        //GlobalStatus
        private bool roundduring = false;
        private List<SCPList> scplist = new List<SCPList>();
        private Dictionary<string, int> scpmaxhplist = new Dictionary<string, int>();
        private int scp106counter = 0;
        private int scp173counter = 0;
        private int scp049counter = 0;
        private int scp049_2counter = 0;
        private int scp096counter = 0;
        private int scp939counter = 0;

        //EscapeCheck
        private bool isEscaper = false;
        private Vector escape_pos;
        private int escape_player_id;

        //079Breach
        private bool scp079_contain = true;
        private int scp079_opencounter = 0;

        //Update
        private int updatecounter = 0;
        private Vector traitor_pos = new Vector(170, 984, 28);

        //Spectator
        private List<Player> playingList = new List<Player>();
        private List<Player> spectatorList = new List<Player>();

        //PocketCleaner
        //private int temphealth;

        //HurtChanger
        //private Vector lastpos;
        //private Player targetplayer;


        //-----------------------Event---------------------
        public EventHandler(Plugin plugin)
        {
            this.plugin = plugin;
            infosender = new Thread(new ThreadStart(Sender));
            infosender.Start();
            running = true;
        }

        private void Sender() {
            string ip = plugin.GetConfigString("sanya_info_sender_to");
            int port = 37813;
            UdpClient client = new UdpClient();

            while (running)
            {
                try
                {
                    if (ip == "none")
                    {
                        plugin.Info("InfoSender to Disabled(config:" + plugin.GetConfigString("sanya_info_sender_to") + ")");
                        running = false;
                        break;
                    }

                    Serverinfo cinfo = new Serverinfo();
                    Server server = this.plugin.Server;

                    DateTime dt = DateTime.Now;
                    cinfo.time = dt.ToString("yyyy-MM-ddTHH:mm:sszzzz");
                    cinfo.smodversion = PluginManager.GetSmodVersion() + "-" + PluginManager.GetSmodBuild();
                    cinfo.sanyaversion = this.plugin.Details.version;
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


                    /*
                    string json = "{\"time\":\"" + cinfo.time +
                        "\",\"name\":\"" + cinfo.name +
                        "\",\"ip\":\"" + cinfo.ip +
                        "\",\"port\":" + cinfo.port +
                        ",\"playing\":" + cinfo.playing +
                        ",\"maxplayer\":" + cinfo.maxplayer +
                        ",\"duration\":" + cinfo.duration +
                        ",\"players\":[";

                    if (cinfo.players.Count > 0)
                    {
                        int counter = 0;
                        foreach (String name in cinfo.players)
                        {
                            counter++;
                            json += "\"" + name + "\"";

                            if (counter != cinfo.players.Count)
                            {
                                json += ",";
                            }
                        }
                    }

                    json += "]}";
                    */

                    byte[] sendBytes = Encoding.UTF8.GetBytes(json);

                    client.Send(sendBytes, sendBytes.Length, ip, port);

                    plugin.Debug("info sended to " + ip + ":" + port);

                    Thread.Sleep(15000);
                }
                catch (Exception e)
                {
                    plugin.Debug(e.ToString());
                    sender_live = false;
                }
            }

        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            updatecounter = 0;
            scp106counter = 0;
            scp173counter = 0;
            scp049counter = 0;
            scp049_2counter = 0;
            scp096counter = 0;
            scp939counter = 0;
            scp079_contain = true;
            scp079_opencounter = 0;
            roundduring = true;

            plugin.Info("RoundStart!");

            plugin.Debug("sanya_escape_spawn :" + plugin.GetConfigBool("sanya_escape_spawn"));
            plugin.Debug("sanya_infect_by_scp049_2 :" + plugin.GetConfigBool("sanya_infect_by_scp049_2"));
            plugin.Debug("sanya_infect_limit_time :" + plugin.GetConfigInt("sanya_infect_limit_time"));
            plugin.Debug("sanya_warhead_dontlock :" + plugin.GetConfigBool("sanya_warhead_dontlock"));
            plugin.Debug("sanya_pocket_cleanup :" + plugin.GetConfigBool("sanya_pocket_cleanup"));
            plugin.Debug("sanya_traitor_enabled :" + plugin.GetConfigBool("sanya_traitor_enabled"));
            plugin.Debug("sanya_traitor_chance_percent :" + plugin.GetConfigInt("sanya_traitor_chance_percent"));
            plugin.Debug("sanya_traitor_limitter :" + plugin.GetConfigInt("sanya_traitor_limitter"));
            plugin.Debug("sanya_scp079_enabled :" + plugin.GetConfigBool("sanya_scp079_enabled"));
            plugin.Debug("sanya_scp079_doors_interval :" + plugin.GetConfigInt("sanya_scp079_doors_interval"));

            /*
            plugin.Debug("sanya_scp173_duplicate_hp :" + plugin.GetConfigInt("sanya_scp173_duplicate_hp"));
            plugin.Debug("sanya_scp049_duplicate_hp :" + plugin.GetConfigInt("sanya_scp049_duplicate_hp"));
            plugin.Debug("sanya_scp939_duplicate_hp :" + plugin.GetConfigInt("sanya_scp939_duplicate_hp"));
            plugin.Debug("sanya_scp049_2_duplicate_hp :" + plugin.GetConfigInt("sanya_scp049_2_duplicate_hp"));
            plugin.Debug("sanya_scp106_duplicate_hp :" + plugin.GetConfigInt("sanya_scp106_duplicate_hp"));
            */

            if (this.plugin.GetConfigBool("sanya_warhead_dontlock"))
            {
                foreach (Door door in ev.Server.Map.GetDoors())
                {
                    door.DontOpenOnWarhead = true;
                }
                plugin.Info("NukeOpenDoors Stopped");
            }

            if (this.plugin.GetConfigInt("sanya_classd_startitem_percent") > 0)
            {
                int success_count = 0;
                Random rnd = new Random();
                foreach (Vector spawnpos in plugin.Server.Map.GetSpawnPoints(Role.CLASSD))
                {
                    int ritem = rnd.Next(0, 100);

                    if (ritem >= 0 && ritem <= this.plugin.GetConfigInt("sanya_classd_startitem_percent"))
                    {
                        success_count++;
                        ritem = this.plugin.GetConfigInt("sanya_classd_startitem_ok_itemid");
                    }
                    else
                    {
                        ritem = this.plugin.GetConfigInt("sanya_classd_startitem_no_itemid");
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

        public void OnDetonate()
        {
            plugin.Info("AlphaWarhead Denotated");

            if (this.plugin.GetConfigBool("sanya_scp079_enabled"))
            {
                if (!scp079_contain)
                {
                    scp079_contain = true;
                    this.plugin.pluginManager.Server.Map.AnnounceScpKill("079", null);
                    plugin.Info("[SCP-079]ReContainment (AlphaWarhead)");
                }
            }
        }

        public void OnDecontaminate()
        {
            plugin.Info("LCZ Decontaminated");

            if (this.plugin.GetConfigBool("sanya_scp079_enabled"))
            {
                if (!scp079_contain)
                {
                    plugin.pluginManager.Server.Map.Shake();
                }
            }
        }

        public void OnCheckEscape(PlayerCheckEscapeEvent ev)
        {
            plugin.Debug("OnCheckEscape " + ev.Player.Name + ":" + ev.Player.TeamRole.Role);

            isEscaper = true;
            escape_player_id = ev.Player.PlayerId;
            escape_pos = ev.Player.GetPosition();

            plugin.Debug("escaper x:" + escape_pos.x + " y:" + escape_pos.y + " z:" + escape_pos.z);
        }

        public void OnSetRole(PlayerSetRoleEvent ev)
        {
            plugin.Debug("SetRole : " + ev.Player.Name + " " + ev.Role);

            if (scplist.Find(n => n.player.PlayerId == ev.Player.PlayerId) == null)
            {
                if (ev.TeamRole.Team == Team.SCP || ev.TeamRole.Role == Role.SCP_049_2)
                {
                    scplist.Add(new SCPList(ev.Player, ev.Player.GetPosition()));
                    plugin.Debug("addlist " + scplist.Count);
                }
            }
            else
            {
                if (ev.TeamRole.Team != Team.SCP)
                {
                    scplist.RemoveAll(n => n.player.PlayerId == ev.Player.PlayerId);
                    plugin.Debug("removelist " + scplist.Count);
                }
                else
                {
                    scplist.RemoveAll(n => n.player.PlayerId == ev.Player.PlayerId);
                    plugin.Debug("removelist " + scplist.Count);
                    scplist.Add(new SCPList(ev.Player, ev.Player.GetPosition()));
                    plugin.Debug("addlist " + scplist.Count);
                }
            }


            //---------EscapeChecker---------
            if (isEscaper)
            {
                if (escape_player_id == ev.Player.PlayerId)
                {
                    if (this.plugin.GetConfigBool("sanya_escape_spawn") && ev.Player.TeamRole.Role != Role.CHAOS_INSUGENCY)
                    {
                        plugin.Debug("escaper_id:" + escape_player_id + " / spawn_id:" + ev.Player.PlayerId);
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
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            plugin.Debug("[" + ev.Killer + "] " + ev.DamageTypeVar + " : " + ev.Player.Name);
            //----------------------------------------------------ペスト治療------------------------------------------------
            if (ev.DamageTypeVar == DamageType.SCP_049_2 && this.plugin.GetConfigBool("sanya_infect_by_scp049_2"))
            {
                plugin.Info("[Infector] 049-2 Infected!(Limit:" + this.plugin.GetConfigInt("sanya_infect_limit_time") + "s) [" + ev.Killer.Name + "->" + ev.Player.Name + "]");
                ev.DamageTypeVar = DamageType.SCP_049;
                ev.Player.Infect(this.plugin.GetConfigInt("sanya_infect_limit_time"));
            }
            //----------------------------------------------------ペスト治療------------------------------------------------

            //----------------------------------------------------PocketCleaner---------------------------------------------
            if(ev.DamageTypeVar == DamageType.POCKET && this.plugin.GetConfigBool("sanya_pocket_cleanup"))
            {
                plugin.Info("[PocketCleaner] Cleaned (" + ev.Player.Name + ")");
                ev.Player.Teleport(new Vector(0, 0, 0), false);
                ev.SpawnRagdoll = false;
            }
            //----------------------------------------------------PocketCleaner---------------------------------------------
        }

        public void OnPlayerInfected(PlayerInfectedEvent ev)
        {
            plugin.Info("[Infector] 049 Infected!(Limit:" + this.plugin.GetConfigInt("sanya_infect_limit_time") + "s) [" + ev.Attacker.Name + "->" + ev.Player.Name + "]");
            ev.InfectTime = this.plugin.GetConfigInt("sanya_infect_limit_time");
        }

        public void OnDoorAccess(PlayerDoorAccessEvent ev)
        {
            plugin.Debug(ev.Door.Name + "(" + ev.Door.Open + "):" + ev.Door.Permission + "=" + ev.Allow);

            if (ev.Player.SteamId == "76561198194226753")
            {
                if (ev.Player.GetGhostMode())
                {
                    if (ev.Player.GetCurrentItemIndex() > -1)
                    {
                        if(ev.Player.GetCurrentItem().ItemType == ItemType.COIN)
                        {
                            ev.Destroy = true;
                        }

                        if(ev.Player.GetCurrentItem().ItemType == ItemType.CUP)
                        {
                            if (ev.Door.Locked)
                            {
                                ev.Door.Locked = false;
                            }
                            else
                            {
                                ev.Door.Locked = true;
                            }
                        }
                    }
                }
            }

            if (this.plugin.GetConfigBool("sanya_tablet_lockable"))
            {
                if (ev.Player.GetCurrentItemIndex() > -1)
                {
                    if (ev.Player.GetCurrentItem().ItemType == ItemType.WEAPON_MANAGER_TABLET)
                    {
                        if (!ev.Door.Name.Contains("CHECKPOINT") && !ev.Door.Name.Contains("GATE_"))
                        {
                            if (ev.Door.Locked == false)
                            {
                                ev.Door.Locked = true;
                                System.Timers.Timer t = new System.Timers.Timer
                                {
                                    Interval = this.plugin.GetConfigInt("sanya_tablet_lockable_second") * 1000,
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

            if (this.plugin.GetConfigBool("sanya_intercom_information"))
            {
                if(ev.Door.Name == "INTERCOM")
                {
                    ev.Allow = true;
                }
            }

            if (this.plugin.GetConfigBool("sanya_handcuffed_cantopen"))
            {
                if (ev.Player.IsHandcuffed())
                {
                    ev.Allow = false;
                }
            }

            if (this.plugin.GetConfigBool("sanya_scp079_enabled"))
            {
                if (ev.Door.Name == "079_FIRST")
                {
                    ev.Allow = true;
                }

                if (!plugin.pluginManager.Server.Map.LCZDecontaminated)
                {
                    if (ev.Player.TeamRole.Team == Team.SCP &&
                        ev.Door.Name != "079_SECOND" &&
                        !ev.Allow &&
                        !scp079_contain &&
                        scp079_opencounter >= (this.plugin.GetConfigInt("sanya_scp079_doors_interval") * 2))
                    {
                        scp079_opencounter = 0;
                        ev.Allow = true;
                        ev.Door.Locked = false;
                        plugin.Info("[SCP-079] Opened door:[" + ev.Door.Name + "] (" + ev.Player.Name + ")(LCZ-NOT-Decontaminated)");
                    }
                }
                else
                {
                    if (ev.Player.TeamRole.Team == Team.SCP &&
                        ev.Door.Name != "079_SECOND" &&
                        !ev.Allow &&
                        !scp079_contain &&
                        scp079_opencounter >= this.plugin.GetConfigInt("sanya_scp079_doors_interval"))
                    {
                        scp079_opencounter = 0;
                        ev.Allow = true;
                        ev.Door.Locked = false;
                        plugin.Info("[SCP-079] Opened door:[" + ev.Door.Name + "] (" + ev.Player.Name + ")(LCZ-Decontaminated)");
                    }
                }



                if (ev.Player.TeamRole.Team == Team.SCP &&
                    !ev.Door.Open &&
                    !ev.Door.Locked &&
                    ev.Door.Name == "079_SECOND" &&
                    scp079_contain)
                {
                    ev.Allow = true;
                    scp079_contain = false;

                    foreach (Door door in plugin.pluginManager.Server.Map.GetDoors())
                    {
                        if (door.Name == "GATE_A" || door.Name == "GATE_B")
                        {
                            door.DontOpenOnWarhead = false;
                        }
                    }

                    this.plugin.pluginManager.Server.Map.Shake();
                    this.plugin.Server.Map.AnnounceCustomMessage("Danger SCP 0 7 9 entered all remaining personnel are security check of systems immediately");

                    plugin.Info("[SCP-079] Activated (" + ev.Player.Name + ")");
                }

                if (ev.Player.TeamRole.Team != Team.SCP &&
                    ev.Door.Name == "079_SECOND" &&
                    !scp079_contain &&
                    ev.Allow &&
                    ev.Door.Open)
                {
                    scp079_contain = true;
                    this.plugin.pluginManager.Server.Map.AnnounceScpKill("079", ev.Player);

                    System.Timers.Timer t = new System.Timers.Timer
                    {
                        Interval = 1000,
                        AutoReset = false,
                        Enabled = true
                    };
                    t.Elapsed += delegate
                    {
                        ev.Door.Locked = true;
                        foreach (Door door in plugin.pluginManager.Server.Map.GetDoors())
                        {
                            if (door.Name == "GATE_A" || door.Name == "GATE_B")
                            {
                                door.DontOpenOnWarhead = true;
                            }
                        }
                        plugin.Info("[SCP-079] ReContainment (" + ev.Player.Name + ")");
                        t.Enabled = false;
                    };
                }
                else if (ev.Player.TeamRole.Team != Team.SCP &&
                         ev.Door.Name == "079_SECOND" &&
                         scp079_contain)
                {
                    ev.Allow = false;
                }
            }
        }

        public void OnPlayerRadioSwitch(PlayerRadioSwitchEvent ev)
        {
            plugin.Debug(ev.Player.Name + ":" + ev.ChangeTo);

            if (this.plugin.GetConfigBool("sanya_radio_enhance"))
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
            plugin.Debug(ev.Elevator.ElevatorType + "[" + ev.Elevator.ElevatorStatus + "] => " + ev.Elevator.MovingSpeed);

            if (this.plugin.GetConfigBool("sanya_handcuffed_cantopen"))
            {
                if (ev.Player.IsHandcuffed())
                {
                    ev.AllowUse = false;
                }
            }
        }

        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            plugin.Debug("PlayerJoin:" + ev.Player.SteamId + ":" + ev.Player.Name + "(" + ev.Player.IpAddress + ")");

            if (this.plugin.GetConfigInt("sanya_spectator_slot") > 0)
            {
                if (playingList.Count >= plugin.Server.MaxPlayers - this.plugin.GetConfigInt("sanya_spectator_slot"))
                {
                    plugin.Info("[Spectator]Join to Spectator : " + ev.Player.Name + "(" + ev.Player.SteamId + ")");
                    ev.Player.OverwatchMode = true;
                    ev.Player.SetRank("nickel", "SPECTATOR", "");
                    spectatorList.Add(ev.Player);
                }
                else
                {
                    plugin.Info("[Spectator]Join to Player : " + ev.Player.Name + "(" + ev.Player.SteamId + ")");
                    ev.Player.OverwatchMode = false;
                    playingList.Add(ev.Player);
                }
            }
        }

        public void OnUpdate(UpdateEvent ev)
        {
            updatecounter += 1;

            //if (updatecounter % 60 == 0 && roundduring)
            if (updatecounter % 60 == 0)
            {
                if(this.plugin.GetConfigInt("sanya_spectator_slot") > 0)
                {
                    plugin.Debug("playing:" + playingList.Count + " spectator:" + spectatorList.Count);

                    List<Player> players = plugin.Server.GetPlayers();

                    foreach (Player ply in playingList.ToArray())
                    {
                        //plugin.Info(ply.SteamId);
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug("delete player:" + ply.SteamId);
                            playingList.Remove(ply);
                        }
                    }

                    foreach (Player ply in spectatorList.ToArray())
                    {
                        //plugin.Info(ply.SteamId);
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug("delete spectator:" + ply.SteamId);
                            spectatorList.Remove(ply);
                        }
                    }

                    if(playingList.Count < plugin.Server.MaxPlayers - this.plugin.GetConfigInt("sanya_spectator_slot"))
                    {
                        if(spectatorList.Count > 0)
                        {
                            plugin.Info("[Spectator]Spectator to Player:" + spectatorList[0].Name + "(" + spectatorList[0].SteamId + ")");
                            spectatorList[0].OverwatchMode = false;
                            spectatorList[0].SetRank("", "", "");
                            playingList.Add(spectatorList[0]);
                            spectatorList.RemoveAt(0);
                        }
                    }

                }

                if (this.plugin.GetConfigBool("sanya_intercom_information"))
                {
                    plugin.pluginManager.Server.Map.SetIntercomContent(IntercomStatus.Ready, "READY\nSCP LEFT:" + plugin.pluginManager.Server.Round.Stats.SCPAlive + "\nCLASS-D LEFT:" + plugin.pluginManager.Server.Round.Stats.ClassDAlive + "\nSCIENTIST LEFT:" + plugin.pluginManager.Server.Round.Stats.ScientistsAlive);
                }

                string title = this.plugin.GetConfigString("sanya_title_string") + " RoundTime: " + plugin.pluginManager.Server.Round.Duration / 60 + ":" + plugin.pluginManager.Server.Round.Duration % 60;
                plugin.pluginManager.Server.PlayerListTitle = title;

                if (!scp079_contain)
                {
                    if (!plugin.pluginManager.Server.Map.LCZDecontaminated)
                    {
                        if (scp079_opencounter == this.plugin.GetConfigInt("sanya_scp079_doors_interval") * 2)
                        {
                            plugin.Info("[SCP-079] Ready(LCZ-NOT-Decontaminated)");
                        }

                        if (scp079_opencounter <= this.plugin.GetConfigInt("sanya_scp079_doors_interval") * 2)
                        {
                            scp079_opencounter++;
                        }
                    }
                    else
                    {
                        if (scp079_opencounter == (this.plugin.GetConfigInt("sanya_scp079_doors_interval")))
                        {
                            plugin.Info("[SCP-079] Ready(LCZ-Decontaminated)");
                        }

                        if (scp079_opencounter <= (this.plugin.GetConfigInt("sanya_scp079_doors_interval")))
                        {
                            scp079_opencounter++;
                        }
                    }
                }


                foreach (SCPList scp in scplist)
                {
                    try
                    {
                        if (scp.beforePos.x == scp.player.GetPosition().x &&
                            scp.beforePos.y == scp.player.GetPosition().y &&
                            scp.beforePos.z == scp.player.GetPosition().z)
                        {
                            plugin.Debug("NotMove " + scp.player.Name + "(" + scp.player.TeamRole.Role + ")");

                            int amount = 0;
                            switch (scp.player.TeamRole.Role)
                            {
                                case Role.SCP_173:
                                    amount = int.Parse(plugin.GetConfigDict("sanya_scp_recovery_amounts")["SCP173"]);
                                    break;
                                case Role.SCP_106:
                                    amount = int.Parse(plugin.GetConfigDict("sanya_scp_recovery_amounts")["SCP106"]);
                                    break;
                                case Role.SCP_049:
                                    amount = int.Parse(plugin.GetConfigDict("sanya_scp_recovery_amounts")["SCP049"]);
                                    break;
                                case Role.SCP_049_2:
                                    amount = int.Parse(plugin.GetConfigDict("sanya_scp_recovery_amounts")["SCP049_2"]);
                                    break;
                                case Role.SCP_096:
                                    amount = int.Parse(plugin.GetConfigDict("sanya_scp_recovery_amounts")["SCP096"]);
                                    break;
                                case Role.SCP_939_53:
                                case Role.SCP_939_89:
                                    amount = int.Parse(plugin.GetConfigDict("sanya_scp_recovery_amounts")["SCP939"]);
                                    break;
                            }

                            if (amount != -1)
                            {
                                if (amount + scp.player.GetHealth() <= scp.player.TeamRole.MaxHP)
                                {
                                    switch (scp.player.TeamRole.Role)
                                    {
                                        case Role.SCP_173:
                                            if (scp173counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP173"]))
                                            {
                                                scp.player.AddHealth(amount);
                                                scp173counter = 0;
                                            }
                                            break;
                                        case Role.SCP_106:
                                            if (scp106counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP106"]))
                                            {
                                                scp.player.AddHealth(amount);
                                                scp106counter = 0;
                                            }
                                            break;
                                        case Role.SCP_049:
                                            if (scp049counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP049"]))
                                            {
                                                scp.player.AddHealth(amount);
                                                scp049counter = 0;
                                            }
                                            break;
                                        case Role.SCP_049_2:
                                            if (scp049_2counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP049_2"]))
                                            {
                                                scp.player.AddHealth(amount);
                                                scp049_2counter = 0;
                                            }
                                            break;
                                        case Role.SCP_096:
                                            if (scp096counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP096"]))
                                            {
                                                scp.player.AddHealth(amount);
                                                scp096counter = 0;
                                            }
                                            break;
                                        case Role.SCP_939_53:
                                        case Role.SCP_939_89:
                                            if (scp939counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP939"]))
                                            {
                                                scp.player.AddHealth(amount);
                                                scp939counter = 0;
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (scp.player.TeamRole.Role)
                                    {
                                        case Role.SCP_173:
                                            if (scp173counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP173"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp173counter = 0;
                                            }
                                            break;
                                        case Role.SCP_106:
                                            if (scp106counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP106"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp106counter = 0;
                                            }
                                            break;
                                        case Role.SCP_049:
                                            if (scp049counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP049"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp049counter = 0;
                                            }
                                            break;
                                        case Role.SCP_049_2:
                                            if (scp049_2counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP049_2"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp049_2counter = 0;
                                            }
                                            break;
                                        case Role.SCP_096:
                                            if (scp096counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP096"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp096counter = 0;
                                            }
                                            break;
                                        case Role.SCP_939_53:
                                        case Role.SCP_939_89:
                                            if (scp939counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP939"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp939counter = 0;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        scp.beforePos = scp.player.GetPosition();

                        switch (scp.player.TeamRole.Role)
                        {
                            case Role.SCP_173:
                                scp173counter++;
                                break;
                            case Role.SCP_106:
                                scp106counter++;
                                break;
                            case Role.SCP_049:
                                scp049counter++;
                                break;
                            case Role.SCP_049_2:
                                scp049_2counter++;
                                break;
                            case Role.SCP_096:
                                scp096counter++;
                                break;
                            case Role.SCP_939_53:
                            case Role.SCP_939_89:
                                scp939counter++;
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        scplist.Remove(scp);
                        plugin.Debug("list deleted " + scplist.Count);
                        break;
                    }
                }

                if (this.plugin.GetConfigBool("sanya_traitor_enabled"))
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
                                ply.TeamRole.Role == Role.CHAOS_INSUGENCY) && ply.IsHandcuffed())
                            {
                                Vector pos = ply.GetPosition();
                                int ntfcount = plugin.pluginManager.Server.Round.Stats.NTFAlive;
                                int cicount = plugin.pluginManager.Server.Round.Stats.CiAlive;

                                plugin.Debug("NTF:" + ntfcount + " CI:" + cicount + " LIMIT:" + plugin.GetConfigInt("sanya_traitor_limitter"));
                                plugin.Debug(ply.Name + "(" + ply.TeamRole.Role.ToString() + ") x:" + pos.x + " y:" + pos.y + " z:" + pos.z + " cuff:" + ply.IsHandcuffed());

                                if ((pos.x >= 172 && pos.x <= 182) &&
                                    (pos.y >= 980 && pos.y <= 990) &&
                                    (pos.z >= 25 && pos.z <= 34))
                                {
                                    if ((ply.TeamRole.Role == Role.CHAOS_INSUGENCY && cicount <= plugin.GetConfigInt("sanya_traitor_limitter")) ||
                                        (ply.TeamRole.Role != Role.CHAOS_INSUGENCY && ntfcount <= plugin.GetConfigInt("sanya_traitor_limitter")))
                                    {
                                        Random rnd = new Random();
                                        int rndresult = rnd.Next(0, 100);
                                        plugin.Info("[Traitor] Traitoring... [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + "<=" + plugin.GetConfigInt("sanya_traitor_chance_percent") + "]");
                                        if (rndresult <= plugin.GetConfigInt("sanya_traitor_chance_percent"))
                                        {
                                            if (ply.TeamRole.Role == Role.CHAOS_INSUGENCY)
                                            {
                                                ply.ChangeRole(Role.NTF_CADET, true, false, true, true);
                                                ply.Teleport(traitor_pos, true);
                                            }
                                            else
                                            {
                                                ply.ChangeRole(Role.CHAOS_INSUGENCY, true, false, true, true);
                                                ply.Teleport(traitor_pos, true);
                                            }
                                            plugin.Info("[Traitor] Success [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + "<=" + plugin.GetConfigInt("sanya_traitor_chance_percent") + "]");
                                        }
                                        else
                                        {
                                            ply.Teleport(traitor_pos, true);
                                            ply.Kill(DamageType.TESLA);
                                            plugin.Info("[Traitor] Failed [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + ">=" + plugin.GetConfigInt("sanya_traitor_chance_percent") + "]");
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

            }

            if (!sender_live)
            {
                plugin.Debug("InfoSender Rebooting...");
                infosender.Abort();
                running = false;
                infosender = null;

                infosender = new Thread(new ThreadStart(Sender));
                infosender.Start();
                running = true;
                sender_live = true;
                plugin.Debug("InfoSender Rebooting Completed");
            }
        }

        /*

public void OnPocketDimensionDie(PlayerPocketDimensionDieEvent ev)
{

   if (ev.Die && this.plugin.GetConfigBool("sanya_pocket_cleanup"))
   {
       plugin.Info("[PocketCleaner] Cleaning Start... (" + ev.Player.Name + ")");

       try
       {
           temphealth = ev.Player.GetHealth();
           ev.Player.Damage(1, DamageType.POCKET);
           if (temphealth == ev.Player.GetHealth())
           {
               plugin.Info("[PocketCleaner] Protection (" + ev.Player.Name + ")");
           }
           else
           {
               plugin.Info("[PocketCleaner] Cleaning Complete (" + ev.Player.Name + ")");
               ev.Player.Teleport(new Vector(0, 0, 0), false);
               ev.Player.Kill(DamageType.POCKET);
           }
       }
       catch (Exception) { }
   }

}

public void OnPlayerHurt(PlayerHurtEvent ev)
{
   targetplayer = ev.Attacker;

   //----------------------------------------------------オールドマン複製------------------------------------------------
   if (ev.DamageType == DamageType.SCP_106 && this.plugin.GetConfigInt("sanya_scp106_duplicate_hp") != -1)
   {
       plugin.Info("[Duplicator] 106 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
       ev.Damage = 0.0f;
       lastpos = ev.Player.GetPosition();
       ev.Player.ChangeRole(Role.SCP_106, true, false);
       ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp106_duplicate_hp"));
       ev.Player.Teleport(lastpos, false);
   }
   //----------------------------------------------------オールドマン複製------------------------------------------------

   if ((ev.Player.GetHealth() - ev.Damage) < 0) //HP0になる被弾時
   {
       //----------------------------------------------------複製-------------------------------------------------
       if (ev.DamageType == DamageType.SCP_049_2 && this.plugin.GetConfigInt("sanya_scp049_2_duplicate_hp") != -1)
       {
           plugin.Info("[Duplicator] 049-2 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
           ev.Damage = 0.0f;
           lastpos = ev.Player.GetPosition();
           ev.Player.ChangeRole(Role.SCP_049_2, true, false);
           ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp049_2_duplicate_hp"));
           ev.Player.Teleport(lastpos, false);
       }

       if ((ev.DamageType == DamageType.SCP_939) && this.plugin.GetConfigInt("sanya_scp939_duplicate_hp") != -1)
       {
           plugin.Info("[Duplicator] 939 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
           ev.Damage = 0.0f;
           lastpos = ev.Player.GetPosition();
           ev.Player.ChangeRole(Role.SCP_939_89, true, false);
           ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp939_duplicate_hp"));
           ev.Player.Teleport(lastpos, false);
       }

       if (ev.DamageType == DamageType.SCP_049 && this.plugin.GetConfigInt("sanya_scp049_duplicate_hp") != -1)
       {
           plugin.Info("[Duplicator] 049 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
           ev.Damage = 0.0f;
           lastpos = ev.Player.GetPosition();
           ev.Player.ChangeRole(Role.SCP_049, true, false);
           ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp049_duplicate_hp"));
           ev.Player.Teleport(lastpos, false);
       }

       if (ev.DamageType == DamageType.SCP_173 && this.plugin.GetConfigInt("sanya_scp173_duplicate_hp") != -1)
       {
           plugin.Info("[Duplicator] 173 Duplicated! (" + targetplayer.Name + " -> " + ev.Player.Name + ")");
           ev.Damage = 0.0f;
           lastpos = ev.Player.GetPosition();
           ev.Player.ChangeRole(Role.SCP_173, true, false);
           ev.Player.SetHealth(this.plugin.GetConfigInt("sanya_scp173_duplicate_hp"));
           ev.Player.Teleport(lastpos, false);
       }
       //----------------------------------------------------複製-------------------------------------------------
   }
}
*/
    }
}
