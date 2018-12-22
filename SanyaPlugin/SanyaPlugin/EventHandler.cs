using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;
using Smod2;
using Smod2.API;
using Smod2.Events;
using Smod2.EventHandlers;
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
        IEventHandlerPlayerHurt,
        IEventHandlerPlayerDie,
        IEventHandlerPocketDimensionDie,
        IEventHandlerInfected,
        IEventHandlerRecallZombie,
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

        //EscapeCheck
        private bool isEscaper = false;
        private Vector escape_pos;
        private int escape_player_id;

        //DoorLocker
        private int doorlock_interval = 0;

        //Update
        private int updatecounter = 0;
        private Vector traitor_pos = new Vector(170, 984, 28);

        //Spectator
        private List<Player> playingList = new List<Player>();
        private List<Player> spectatorList = new List<Player>();

        //-----------------------Event---------------------
        public EventHandler(Plugin plugin)
        {
            this.plugin = plugin;
            infosender = new Thread(new ThreadStart(Sender));
            infosender.Start();
            running = true;
        }

        private void Sender()
        {
            UdpClient client = new UdpClient();

            while (running)
            {
                try
                {
                    string ip = plugin.GetConfigString("sanya_info_sender_to");
                    int port = 37813;

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
                    //cinfo.name = server.Name;
                    cinfo.name = ConfigManager.Manager.Config.GetStringValue("server_name", plugin.Server.Name);
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
            roundduring = true;

            plugin.Info("RoundStart!");

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
        }

        public void OnDecontaminate()
        {
            plugin.Info("LCZ Decontaminated");
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

            //---------EscapeChecker---------
            if (isEscaper)
            {
                if (escape_player_id == ev.Player.PlayerId)
                {
                    if (this.plugin.GetConfigBool("sanya_escape_spawn") && ev.Player.TeamRole.Role != Role.CHAOS_INSURGENCY)
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

        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            if(ev.DamageType == DamageType.USP)
            {
                ev.Damage *= 2.0f;
            }
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            plugin.Debug("[" + ev.Killer + "] " + ev.DamageTypeVar + " : " + ev.Player.Name);

            //----------------------------------------------------キル回復------------------------------------------------
            Dictionary<string, string> scp_recamount = this.plugin.GetConfigDict("sanya_scp_actrecovery_amounts");
  
            //############## SCP-173 ###############
            if (ev.DamageTypeVar == DamageType.SCP_173 && 
                ev.Killer.TeamRole.Role == Role.SCP_173 && 
                int.Parse(scp_recamount["SCP_173"]) > 0)
            {
                if(ev.Killer.GetHealth() + int.Parse(scp_recamount["SCP_173"]) < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(int.Parse(scp_recamount["SCP_173"]));
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }

            //############## SCP-096 ###############
            if (ev.DamageTypeVar == DamageType.SCP_096 &&
                ev.Killer.TeamRole.Role == Role.SCP_096 &&
                int.Parse(scp_recamount["SCP_096"]) > 0)
            {
                if (ev.Killer.GetHealth() + int.Parse(scp_recamount["SCP_096"]) < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(int.Parse(scp_recamount["SCP_096"]));
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }

            //############## SCP-939 ###############
            if (ev.DamageTypeVar == DamageType.SCP_939 &&
                (ev.Killer.TeamRole.Role == Role.SCP_939_53) || (ev.Killer.TeamRole.Role == Role.SCP_939_89) &&
                int.Parse(scp_recamount["SCP_939"]) > 0)
            {
                if (ev.Killer.GetHealth() + int.Parse(scp_recamount["SCP_939"]) < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(int.Parse(scp_recamount["SCP_939"]));
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }

            //############## SCP-049-2 ###############
            if (ev.DamageTypeVar == DamageType.SCP_049_2 &&
                ev.Killer.TeamRole.Role == Role.SCP_049_2 &&
                int.Parse(scp_recamount["SCP_049_2"]) > 0)
            {
                if (ev.Killer.GetHealth() + int.Parse(scp_recamount["SCP_049_2"]) < ev.Killer.TeamRole.MaxHP)
                {
                    ev.Killer.AddHealth(int.Parse(scp_recamount["SCP_049_2"]));
                }
                else
                {
                    ev.Killer.SetHealth(ev.Killer.TeamRole.MaxHP);
                }
            }
            //----------------------------------------------------キル回復------------------------------------------------

            //----------------------------------------------------ペスト治療------------------------------------------------
            if (ev.DamageTypeVar == DamageType.SCP_049_2 && this.plugin.GetConfigBool("sanya_infect_by_scp049_2"))
            {
                plugin.Info("[Infector] 049-2 Infected!(Limit:" + this.plugin.GetConfigInt("sanya_infect_limit_time") + "s) [" + ev.Killer.Name + "->" + ev.Player.Name + "]");
                ev.DamageTypeVar = DamageType.SCP_049;
                ev.Player.Infect(this.plugin.GetConfigInt("sanya_infect_limit_time"));
            }
            //----------------------------------------------------ペスト治療------------------------------------------------

            //----------------------------------------------------PocketCleaner---------------------------------------------
            if (ev.DamageTypeVar == DamageType.POCKET && this.plugin.GetConfigBool("sanya_pocket_cleanup"))
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

        public void OnPocketDimensionDie(PlayerPocketDimensionDieEvent ev)
        {
            plugin.Debug("OnPocketDie:" + ev.Player.Name);

            //------------------------------ディメンション死亡回復(SCP-106)-----------------------------------------
            Dictionary<string, string> scp_recamount = this.plugin.GetConfigDict("sanya_scp_actrecovery_amounts");

            if (int.Parse(scp_recamount["SCP_106"]) > 0)
            {
                try
                {
                    foreach (Player ply in plugin.Server.GetPlayers())
                    {
                        if (ply.TeamRole.Role == Role.SCP_106)
                        {
                            if (ply.GetHealth() + int.Parse(scp_recamount["SCP_106"]) < ply.TeamRole.MaxHP)
                            {
                                ply.AddHealth(int.Parse(scp_recamount["SCP_106"]));
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
            plugin.Debug("recall:" + ev.Player.Name + " -> " + ev.Target.Name);

            Dictionary<string, string> scp_recamount = this.plugin.GetConfigDict("sanya_scp_actrecovery_amounts");

            //----------------------治療回復SCP-049---------------------------------
            if (ev.Player.TeamRole.Role == Role.SCP_049 &&
                int.Parse(scp_recamount["SCP_049"]) > 0)
            {
                if (ev.Player.GetHealth() + int.Parse(scp_recamount["SCP_049"]) < ev.Player.TeamRole.MaxHP)
                {
                    ev.Player.AddHealth(int.Parse(scp_recamount["SCP_049"]));
                }
                else
                {
                    ev.Player.SetHealth(ev.Player.TeamRole.MaxHP);
                }
            }
        }

        public void OnDoorAccess(PlayerDoorAccessEvent ev)
        {
            plugin.Debug(ev.Door.Name + "(" + ev.Door.Open + "):" + ev.Door.Permission + "=" + ev.Allow);

            if (this.plugin.GetConfigBool("sanya_door_lockable"))
            {
                if (doorlock_interval >= this.plugin.GetConfigInt("sanya_door_lockable_interval"))
                {
                    if (ev.Player.TeamRole.Role == Role.NTF_COMMANDER)
                    {
                        if (ev.Player.GetCurrentItemIndex() > -1)
                        {
                            if (ev.Player.GetCurrentItem().ItemType == ItemType.DISARMER)
                            {
                                if (!ev.Door.Name.Contains("CHECKPOINT") && !ev.Door.Name.Contains("GATE_"))
                                {
                                    if (ev.Door.Locked == false)
                                    {
                                        plugin.Info("[DoorLock] " + ev.Player.Name + " lock " + ev.Door.Name);
                                        doorlock_interval = 0;
                                        ev.Door.Locked = true;
                                        System.Timers.Timer t = new System.Timers.Timer
                                        {
                                            Interval = this.plugin.GetConfigInt("sanya_door_lockable_second") * 1000,
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
            }

            if (this.plugin.GetConfigBool("sanya_intercom_information"))
            {
                if (ev.Door.Name == "INTERCOM")
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
            plugin.Info("[PlayerJoin] " + ev.Player.Name + "(" + ev.Player.SteamId + ")[" + ev.Player.IpAddress + "]");

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
                if (this.plugin.GetConfigInt("sanya_spectator_slot") > 0)
                {
                    plugin.Debug("playing:" + playingList.Count + " spectator:" + spectatorList.Count);

                    List<Player> players = plugin.Server.GetPlayers();

                    foreach (Player ply in playingList.ToArray())
                    {
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug("delete player:" + ply.SteamId);
                            playingList.Remove(ply);
                        }
                    }

                    foreach (Player ply in spectatorList.ToArray())
                    {
                        if (players.FindIndex(item => item.SteamId == ply.SteamId) == -1)
                        {
                            plugin.Debug("delete spectator:" + ply.SteamId);
                            spectatorList.Remove(ply);
                        }
                    }

                    if (playingList.Count < plugin.Server.MaxPlayers - this.plugin.GetConfigInt("sanya_spectator_slot"))
                    {
                        if (spectatorList.Count > 0)
                        {
                            plugin.Info("[Spectator]Spectator to Player:" + spectatorList[0].Name + "(" + spectatorList[0].SteamId + ")");
                            spectatorList[0].OverwatchMode = false;
                            spectatorList[0].SetRank();
                            playingList.Add(spectatorList[0]);
                            spectatorList.RemoveAt(0);
                        }
                    }

                }

                if (this.plugin.GetConfigBool("sanya_door_lockable"))
                {
                    if (doorlock_interval < this.plugin.GetConfigInt("sanya_door_lockable_interval"))
                    {
                        doorlock_interval++;
                    }
                }

                if (this.plugin.GetConfigBool("sanya_intercom_information"))
                {
                    plugin.pluginManager.Server.Map.SetIntercomContent(IntercomStatus.Ready, "READY\nSCP LEFT:" + plugin.pluginManager.Server.Round.Stats.SCPAlive + "\nCLASS-D LEFT:" + plugin.pluginManager.Server.Round.Stats.ClassDAlive + "\nSCIENTIST LEFT:" + plugin.pluginManager.Server.Round.Stats.ScientistsAlive);
                }

                if (this.plugin.GetConfigBool("sanya_title_timer"))
                {
                    string title = ConfigManager.Manager.Config.GetStringValue("player_list_title", "Unnamed Server") + " RoundTime: " + plugin.pluginManager.Server.Round.Duration / 60 + ":" + plugin.pluginManager.Server.Round.Duration % 60;
                    plugin.pluginManager.Server.PlayerListTitle = title;
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
                                ply.TeamRole.Role == Role.CHAOS_INSURGENCY) && ply.IsHandcuffed())
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
                                    if ((ply.TeamRole.Role == Role.CHAOS_INSURGENCY && cicount <= plugin.GetConfigInt("sanya_traitor_limitter")) ||
                                        (ply.TeamRole.Role != Role.CHAOS_INSURGENCY && ntfcount <= plugin.GetConfigInt("sanya_traitor_limitter")))
                                    {
                                        Random rnd = new Random();
                                        int rndresult = rnd.Next(0, 100);
                                        plugin.Info("[Traitor] Traitoring... [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + "<=" + plugin.GetConfigInt("sanya_traitor_chance_percent") + "]");
                                        if (rndresult <= plugin.GetConfigInt("sanya_traitor_chance_percent"))
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
    }
}
