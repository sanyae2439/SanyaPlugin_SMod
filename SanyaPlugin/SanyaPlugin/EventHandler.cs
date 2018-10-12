using System;
using System.Collections.Generic;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.Config;
using Smod2.EventSystem.Events;

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

    class EventHandler :
        IEventHandlerRoundStart,
        IEventHandlerRoundEnd,
        IEventHandlerLCZDecontaminate,
        IEventHandlerWarheadDetonate,
        IEventHandlerCheckEscape,
        IEventHandlerSetRole,
        IEventHandlerPocketDimensionDie,
        IEventHandlerInfected,
        IEventHandlerPlayerHurt,
        IEventHandlerDoorAccess,
        IEventHandlerUpdate
    {
        private Plugin plugin;

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

        //HurtChanger
        private Vector lastpos;
        private Player targetplayer;

        //EscapeCheck
        private bool isEscaper = false;
        private Vector escape_pos;
        private int escape_player_id;

        //PocketCleaner
        private int temphealth;

        //079Breach
        private bool scp079_contain = true;
        private int scp079_opencounter = 0;

        //Update
        private int updatecounter = 0;
        private Vector traitor_pos = new Vector(173, 984, 28);


        //-----------------------Event---------------------
        public EventHandler(Plugin plugin)
        {
            this.plugin = plugin;
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
            plugin.Debug("sanya_scp173_duplicate_hp :" + plugin.GetConfigInt("sanya_scp173_duplicate_hp"));
            plugin.Debug("sanya_scp049_duplicate_hp :" + plugin.GetConfigInt("sanya_scp049_duplicate_hp"));
            plugin.Debug("sanya_scp939_duplicate_hp :" + plugin.GetConfigInt("sanya_scp939_duplicate_hp"));
            plugin.Debug("sanya_scp049_2_duplicate_hp :" + plugin.GetConfigInt("sanya_scp049_2_duplicate_hp"));
            plugin.Debug("sanya_scp106_duplicate_hp :" + plugin.GetConfigInt("sanya_scp106_duplicate_hp"));

            if (this.plugin.GetConfigBool("sanya_warhead_dontlock"))
            {
                foreach (Door door in ev.Server.Map.GetDoors())
                {
                    door.DontOpenOnWarhead = true;
                }
                plugin.Info("NukeOpenDoors Stopped");
            }
        }

        public void OnRoundEnd(RoundEndEvent ev)
        {
            if (roundduring)
            {
                plugin.Info("Round Ended [" + ev.Status + "]");
                plugin.Info("Class-D:" + ev.Round.Stats.ClassDAlive + " Scientist:" + ev.Round.Stats.ScientistsAlive + " NTF:" + ev.Round.Stats.NTFAlive + " SCP:" + ev.Round.Stats.SCPAlive);
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
                //----------------------------------------------------ペスト治療------------------------------------------------
                if (ev.DamageType == DamageType.SCP_049_2 && this.plugin.GetConfigBool("sanya_infect_by_scp049_2"))
                {
                    plugin.Info("[Infector] 049-2 Infected!(Limit:" + this.plugin.GetConfigInt("sanya_infect_limit_time") + "s) [" + targetplayer.Name + "->" + ev.Player.Name + "]");
                    ev.DamageType = DamageType.SCP_049;
                    ev.Player.Infect(this.plugin.GetConfigInt("sanya_infect_limit_time"));
                }
                //----------------------------------------------------ペスト治療------------------------------------------------

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

        public void OnPlayerInfected(PlayerInfectedEvent ev)
        {
            plugin.Info("[InfectChecker] 049 Infected!(Limit:" + this.plugin.GetConfigInt("sanya_infect_limit_time") + "s) [" + ev.Attacker.Name + "->" + ev.Player.Name + "]");
            ev.InfectTime = this.plugin.GetConfigInt("sanya_infect_limit_time");
        }

        public void OnDoorAccess(PlayerDoorAccessEvent ev)
        {
            plugin.Debug(ev.Door.Name + "(" + ev.Door.Open + "):" + ev.Door.Permission + "=" + ev.Allow);
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

        public void OnUpdate(UpdateEvent ev)
        {
            updatecounter += 1;

            //if (updatecounter % 60 == 0 && roundduring)
            if (updatecounter % 60 == 0)
            {
                string title = this.plugin.GetConfigString("sanya_title_string") + " RoundTime: " + plugin.pluginManager.Server.Round.Duration/60 + ":" + plugin.pluginManager.Server.Round.Duration%60;
                plugin.pluginManager.Server.PlayerListTitle = title;

                if (!scp079_contain)
                {
                    if(!plugin.pluginManager.Server.Map.LCZDecontaminated)
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
                                                scp939counter = 0;
                                            }
                                            break;
                                        case Role.SCP_106:
                                            if (scp106counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP106"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp939counter = 0;
                                            }
                                            break;
                                        case Role.SCP_049:
                                            if (scp049counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP049"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp939counter = 0;
                                            }
                                            break;
                                        case Role.SCP_049_2:
                                            if (scp049_2counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP049_2"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp939counter = 0;
                                            }
                                            break;
                                        case Role.SCP_096:
                                            if (scp096counter >= int.Parse(plugin.GetConfigDict("sanya_scp_recovery_durations")["SCP096"]))
                                            {
                                                scp.player.SetHealth(scp.player.TeamRole.MaxHP);
                                                scp939counter = 0;
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
                                int cicount = 0;
                                foreach (Player plycount in plugin.pluginManager.Server.GetPlayers())
                                {
                                    if (plycount.TeamRole.Role == Role.CHAOS_INSUGENCY)
                                    {
                                        cicount++;
                                    }
                                }

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
                                                    ply.ChangeRole(Role.NTF_CADET, true, false,true);
                                                    ply.Teleport(traitor_pos, true);
                                                }
                                                else
                                                {
                                                    ply.ChangeRole(Role.CHAOS_INSUGENCY, true, false,true);
                                                    ply.Teleport(traitor_pos, true);
                                                }
                                                plugin.Info("[Traitor] Success [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + "<=" + plugin.GetConfigInt("sanya_traitor_chance_percent") + "]");
                                            }
                                            else
                                            {
                                                ply.Teleport(traitor_pos, true);
                                                ply.Kill(DamageType.TESLA);
                                                plugin.Info("[Traitor] Failed [" + ply.Name + ":" + ply.TeamRole.Role + ":" + rndresult + "<=" + plugin.GetConfigInt("sanya_traitor_chance_percent") + "]");
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
        }
    }
}
