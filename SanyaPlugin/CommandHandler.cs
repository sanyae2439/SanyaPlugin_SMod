using System.Collections.Generic;
using Smod2.API;
using Smod2.Commands;
using UnityEngine;

namespace SanyaPlugin
{
    public class CommandHandler : ICommandHandler
    {
        private SanyaPlugin plugin;

        public CommandHandler(SanyaPlugin plugin)
        {
            this.plugin = plugin;

            /*
            int count1 = 0;
            int count2 = 0;
            foreach (Scp079Interactable items in Interface079.singleton.allInteractables)
            {
                if (items.type == Scp079Interactable.InteractableType.Lockdown)
                {
                    count1++;
                    foreach (Scp079Interactable.ZoneAndRoom zar in items.currentZonesAndRooms)
                    {
                        count2++;
                        Interface079.lply.CallRpcFlickerLights(zar.currentRoom, zar.currentZone);
                        plugin.Info(zar.currentRoom + "/" + zar.currentZone);
                    }
                }
            }

            /*
            FlickerableLight[] array = UnityEngine.Object.FindObjectsOfType<FlickerableLight>();

            foreach (FlickerableLight items in array)
            {
                Interface079.lply.CallRpcFlickerLights()
            }


            List<Room> temprooms = plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA);
            List<Room> ent = temprooms.FindAll(items => { return items.ZoneType == ZoneType.ENTRANCE; });
            List<Room> lcz = temprooms.FindAll(items => { return items.ZoneType == ZoneType.LCZ; });
            List<Room> hcz = temprooms.FindAll(items => { return items.ZoneType == ZoneType.HCZ; });
            List<Room> und = temprooms.FindAll(items => { return items.ZoneType == ZoneType.UNDEFINED; });
            plugin.Info("ent:" + ent.Count + " hcz:" + hcz.Count + " lcz:" + lcz.Count + " und:" + und.Count);

            ent.ForEach(items => { plugin.Info(items.ZoneType + ":" + items.RoomType); });
            hcz.ForEach(items => { plugin.Info(items.ZoneType + ":" + items.RoomType); });
            lcz.ForEach(items => { plugin.Info(items.ZoneType + ":" + items.RoomType); });
            */
        }

        public string GetCommandDescription()
        {
            return "SanyaPlugin Command";
        }

        public string GetUsage()
        {
            return "SANYA < PING / OVERRIDE| AMMO | BLACKOUT | GEN (UNLOCK/OPEN/CLOSE/ACT) | EV | TESLA (I) | HELI | VAN | NEXT (CI/MTF) | SPAWN | 914 (USE/CHANGE) | 096 | 939 | 079 (LEVEL (1-5)/AP) | SHAKE >";
        }

        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if(args.Length > 0)
            {
                if(args[0] == "reload")
                {
                    plugin.LoadPlayersData();

                    return new string[] { "Player Data Reload!" };
                }
                else if(args[0] == "blackout")
                {
                    List<Room> rooms = new List<Room>(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA)).FindAll(x => x.ZoneType == ZoneType.LCZ);
                    foreach(Room r in rooms)
                    {
                        r.FlickerLights();
                    }
                    Generator079.mainGenerator.CallRpcOvercharge();

                    return new string[] { "blackout success." };
                }
                else if(args[0] == "gen")
                {
                    if(args.Length > 1)
                    {
                        if(args[1] == "unlock")
                        {
                            foreach(Generator items in plugin.Server.Map.GetGenerators())
                            {
                                items.Unlock();
                            }
                            return new string[] { "gen unlock." };
                        }
                        else if(args[1] == "open")
                        {
                            foreach(Generator items in plugin.Server.Map.GetGenerators())
                            {
                                if(!items.Engaged)
                                {
                                    items.Open = true;
                                }
                            }
                            return new string[] { "gen open." };
                        }
                        else if(args[1] == "close")
                        {
                            foreach(Generator items in plugin.Server.Map.GetGenerators())
                            {
                                items.Open = false;
                            }
                            return new string[] { "gen close." };
                        }
                        else if(args[1] == "act")
                        {
                            foreach(Generator items in plugin.Server.Map.GetGenerators())
                            {
                                float engagecount = 6.0f;
                                if(!items.Engaged)
                                {
                                    items.TimeLeft = engagecount--;
                                    items.HasTablet = true;
                                }
                            }
                            return new string[] { "gen activate." };
                        }
                    }
                }
                else if(args[0] == "ev")
                {
                    foreach(Elevator ev in plugin.Server.Map.GetElevators())
                    {
                        ev.Use();
                    }
                    return new string[] { "EV used." };
                }
                else if(args[0] == "tesla")
                {
                    bool isInstant = false;

                    if(args.Length > 1)
                    {
                        if(args[1] == "i")
                        {
                            isInstant = true;
                        }
                    }

                    foreach(Smod2.API.TeslaGate tesla in plugin.Server.Map.GetTeslaGates())
                    {
                        tesla.Activate(isInstant);
                    }
                    return new string[] { "tesla activated." };
                }
                else if(args[0] == "shake")
                {
                    plugin.Server.Map.Shake();

                    return new string[] { "map shaking." };
                }
                else if(args[0] == "heli")
                {
                    SanyaPlugin.CallVehicle(false);

                    return new string[] { "heli moved." };
                }
                else if(args[0] == "van")
                {
                    SanyaPlugin.CallVehicle(true);

                    return new string[] { "van spawned." };
                }
                else if(args[0] == "next")
                {
                    if(args.Length > 1)
                    {
                        GameObject host = GameObject.Find("Host");
                        MTFRespawn respawn = host.GetComponent<MTFRespawn>();
                        if(args[1] == "ci")
                        {
                            respawn.nextWaveIsCI = true;
                            return new string[] { $"nextIsCi:{respawn.nextWaveIsCI}" };
                        }
                        else if(args[1] == "mtf" || args[1] == "ntf")
                        {
                            respawn.nextWaveIsCI = false;
                            return new string[] { $"nextisCi:{respawn.nextWaveIsCI}" };
                        }
                    }
                }
                else if(args[0] == "spawn")
                {
                    GameObject host = GameObject.Find("Host");
                    MTFRespawn respawn = host.GetComponent<MTFRespawn>();

                    if(respawn.nextWaveIsCI)
                    {
                        respawn.timeToNextRespawn = 14f;
                        return new string[] { $"SpawnSet. nextIsCi:{respawn.nextWaveIsCI}" };
                    }
                    else
                    {
                        respawn.timeToNextRespawn = 19f;
                        return new string[] { $"SpawnSet. nextIsCi:{respawn.nextWaveIsCI}" };
                    }
                }
                else if(args[0] == "override")
                {
                    Player ply = sender as Player;
                    if(ply != null)
                    {
                        SanyaPlugin.scp_override_steamid = ply.SteamId;
                    }

                    return new string[] { $"set ok:{SanyaPlugin.scp_override_steamid}" };
                }
                else if(args[0] == "ammo")
                {
                    Player ply = sender as Player;
                    if(ply != null)
                    {
                        ply.SetAmmo(AmmoType.DROPPED_5, 999);
                        ply.SetAmmo(AmmoType.DROPPED_7, 999);
                        ply.SetAmmo(AmmoType.DROPPED_9, 999);
                    }

                    return new string[] { $"Ammo set full." };
                }
                else if(args[0] == "914")
                {
                    if(args.Length > 1)
                    {
                        if(args[1] == "use")
                        {
                            SanyaPlugin.Call914Use();
                            return new string[] { "914 used." };
                        }
                        else if(args[1] == "change")
                        {
                            SanyaPlugin.Call914Change();
                            return new string[] { "914 changed." };
                        }
                    }
                }
                else if(args[0] == "939")
                {
                    SanyaPlugin.Call939CanSee();

                    return new string[] { "939 can all see." };
                }
                else if(args[0] == "096")
                {
                    Scp096PlayerScript.instance.IncreaseRage(1f);

                    return new string[] { "096 has Raged." };
                }
                else if(args[0] == "079")
                {
                    if(args.Length > 2)
                    {
                        if(args[1] == "level")
                        {
                            foreach(Player player in plugin.Server.GetPlayers(Role.SCP_079))
                            {
                                player.Scp079Data.Level = Mathf.Clamp(int.Parse(args[2]) - 1, 0, 4);
                                player.Scp079Data.ShowLevelUp(Mathf.Clamp(int.Parse(args[2]) - 1, 0, 4));
                            }
                            return new string[] { $"079 Level Set to:{Mathf.Clamp(int.Parse(args[2]), 1, 5)}" };
                        }
                    }
                    else if(args.Length > 1)
                    {
                        if(args[1] == "ap")
                        {
                            foreach(Player player in plugin.Server.GetPlayers(Role.SCP_079))
                            {
                                player.Scp079Data.AP = player.Scp079Data.MaxAP;
                            }
                            return new string[] { "079 AP MAX." };
                        }
                    }
                }
                else if(args[0] == "flagtest")
                {
                    if(SanyaPlugin.test)
                    {
                        SanyaPlugin.test = false;
                    }
                    else
                    {
                        SanyaPlugin.test = true;
                    }
                    plugin.Error($"test:{SanyaPlugin.test}");

                    return new string[] { $"test:{SanyaPlugin.test}" };
                }
                else if(args[0] == "ping")
                {
                    List<string> pinglist = new List<string>();
                    byte b;

                    foreach(Player player in plugin.Server.GetPlayers())
                    {
                        UnityEngine.Networking.NetworkConnection conn = (player.GetGameObject() as GameObject).GetComponent<NicknameSync>().connectionToClient;
                        pinglist.Add($"Name: {player.Name} IP: {player.IpAddress} Ping: {UnityEngine.Networking.NetworkTransport.GetCurrentRTT(conn.hostId, conn.connectionId, out b)}ms");
                    }

                    return pinglist.ToArray();
                }
                else if(args[0] == "now")
                {
                    return new string[] { TimeBehaviour.CurrentTimestamp().ToString() };
                }
                else if(args[0] == "test")
                {
                    Player ply = sender as Player;
                    GameObject gameObject = null;
                    GameObject host = GameObject.Find("Host");
                    System.Random rnd = new System.Random();

                    if(ply != null)
                    {
                        gameObject = ply.GetGameObject() as GameObject;
                    }

                    //foreach (Camera079 item in Scp079PlayerScript.allCameras)
                    //{
                    //    if(item.cameraName.Contains("ICOM"))
                    //        plugin.Debug($"Name:{item.cameraName}");
                    //}

                    //foreach(Smod2.API.Player p in plugin.Server.GetPlayers())
                    //{
                    //    FootstepSync foots = (p.GetGameObject() as UnityEngine.GameObject).GetComponent<FootstepSync>();

                    //    foots.CallCmdSyncFoot(true);
                    //}

                    //if(args.Length > 1)
                    //{
                    //    SanyaPlugin.CallAmbientSound(int.Parse(args[1]));
                    //}  

                    //RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();

                    //foreach(var i in rnde.pickups)
                    //{
                    //    plugin.Info($"{i.itemID} {i.posID}");
                    //}

                    //foreach(var i in rnde.posIds)
                    //{
                    //    plugin.Info($"{i.index} {i.posID} {i.position.position}");
                    //}

                    //(ply.GetGameObject() as UnityEngine.GameObject).GetComponent<FlashEffect>().CallCmdBlind(true);

                    //Scp049PlayerScript s049 = gameObject.GetComponent<Scp049PlayerScript>();
                    //Vector3 position = s049.plyCam.transform.position;
                    //Vector3 forward = s049.plyCam.transform.forward;

                    //plugin.Debug($"Raycast...");
                    //RaycastHit raycastHit;
                    //if (Physics.Raycast(position, forward, out raycastHit, 500f, 262144))
                    //{
                    //    plugin.Error($"name:{raycastHit.transform.name} parent:{raycastHit.transform.parent.name} root:{raycastHit.transform.root.name}");
                    //}
                    //else
                    //{
                    //    plugin.Warn($"not hit(raycast)");
                    //}

                    //plugin.Debug($"CheckGround...");
                    //Vector3 pos = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y - 0.8f, gameObject.transform.position.z);
                    //Collider[] hits = Physics.OverlapBox(pos, FallDamage.GroundCheckSize, new Quaternion(0f, 0f, 0f, 0f), FallDamage.staticGroundMask);
                    //if (hits.Length != 0)
                    //{
                    //    foreach (var i in hits)
                    //    {
                    //        plugin.Error($"name:{i.transform.name} parent:{i.transform.parent.name} root:{i.transform.root.name}");
                    //    }
                    //}
                    //else
                    //{
                    //    plugin.Warn($"not hit(ground)");
                    //}

                    //foreach (var i in GameObject.FindObjectsOfType<TeslaGate>())
                    //{
                    //    plugin.Debug($"{i.killerMask.ToString()}");
                    //}

                    //for (int i = 0; i < 32; i++)
                    //{
                    //    plugin.Debug($"Layer[{i}]{LayerMask.LayerToName(i)}");
                    //    if (((1 << i) & 1208246273) != 0)
                    //    {
                    //        plugin.Warn($"1208246273 in [{i}]");
                    //    }
                    //    if (((1 << i) & 1207976449) != 0)
                    //    {
                    //        plugin.Warn($"1207976449 in [{i}]");
                    //    }
                    //}

                    //plugin.Debug($"{LayerMask.GetMask(new string[] { "CCTV" })}");

                    //CharacterClassManager ccm = gameObject.GetComponent<CharacterClassManager>();
                    //PlayerStats.HitInfo info = new PlayerStats.HitInfo(1f, ply.Name, DamageTypes.None, ply.PlayerId);
                    //gameObject.GetComponent<RagdollManager>().SpawnRagdoll(gameObject.transform.position, gameObject.transform.rotation, ccm.curClass, info, false,
                    //    gameObject.GetComponent<Dissonance.Integrations.UNet_HLAPI.HlapiPlayer>().PlayerId, gameObject.GetComponent<NicknameSync>().myNick,
                    //    gameObject.GetComponent<RemoteAdmin.QueryProcessor>().PlayerId, gameObject);

                    //plugin.Debug($"{GameObject.FindObjectOfType<TeslaGate>().killerMask.value}");

                    //RandomItemSpawner rnde = UnityEngine.GameObject.FindObjectOfType<RandomItemSpawner>();
                    //foreach (var pos in rnde.posIds)
                    //{
                    //    plugin.Warn($"[{pos.index}] {pos.posID} -> [{pos.position.position}]");
                    //}

                    //plugin.Debug($"{(ply.GetCurrentItem().GetComponent() as Inventory).items[(ply.GetCurrentItem().GetComponent() as Inventory).curItem].}");

                    //var outside = GameObject.FindObjectOfType<AlphaWarheadOutsitePanel>();

                    //if(outside != null)
                    //{
                    //    outside.SetKeycardState(false);
                    //}

                    //GrenadeManager gre = GameObject.FindObjectOfType<GrenadeManager>();
                    //foreach (var i in GrenadeManager.grenadesOnScene)
                    //{
                    //    plugin.Error($"{i.id}");
                    //    gre.CallRpcExplode(i.id, ply.PlayerId);
                    //}

                    //GrenadeManager gre = GameObject.FindObjectOfType<GrenadeManager>();
                    //foreach(var i in gre.availableGrenades)
                    //{
                    //    plugin.Error($"{i.apiName}[{i.inventoryID}]:{i.timeUnitilDetonation}");
                    //}

                    //GrenadeManager gre = GameObject.FindObjectOfType<GrenadeManager>();
                    //foreach (var i in GrenadeManager.grenadesOnScene)
                    //{
                    //    plugin.Error($"{i.id}:{i.transform.position}");
                    //}

                    //plugin.Error($"return:{MEC.Timing.KillCoroutines("FollowingGrenade")}");

                    //gameObject.GetComponent<PlyMovementSync>().SetAllowInput(SanyaPlugin.test);

                    //for(var i = 0; i < Scp079PlayerScript.allCameras.Length; i++)
                    //{
                    //    plugin.Warn($"[{i}]{Scp079PlayerScript.allCameras[i].cameraName}");
                    //}

                    //plugin.Error($"{plugin.Server.GetAppFolder(false,true)}");
                    //plugin.Error($"{plugin.Server.GetAppFolder(true,true)}");
                    //plugin.Warn($"{gameObject.GetComponent<ServerRoles>().GetUncoloredRoleString()}:{gameObject.GetComponent<ServerRoles>().MyColor}");

                    //plugin.Error($"Count:{plugin.playersData.Count}");
                    //foreach(PlayerData player in plugin.playersData)
                    //{
                    //    plugin.Error($"{player.steamid}:{player.level}:{player.exp}");
                    //}

                    //foreach(PlayerData data in plugin.playersData)
                    //{
                    //    plugin.Warn($"{data.steamid}:Level{data.level}({data.exp}EXP/Next:{Mathf.Clamp(data.level*3-data.exp,0,data.level*3-data.exp)})");
                    //}

                    //MEC.Timing.RunCoroutine(plugin._CheckIsLimitedSteam(ply), MEC.Segment.FixedUpdate);

                    //ServerConsole.Disconnect(gameObject, plugin.steam_kick_limited_message);

                    //plugin.Error($"{host.GetComponent<MTFRespawn>().timeToNextRespawn}");

                    //foreach(var i in gameObject.GetComponent<Medkit>().Medkits)
                    //{
                    //    plugin.Error($"{i.InventoryID}:{i.Label}:{i.MaximumHealthRegeneration}:{i.MinimumHealthRegeneration}");
                    //}

                    //gameObject.GetComponent<Inventory>().NetworkcurItem = -1;

                    //plugin.Error($"{LayerMask.GetMask(new string[] { "Ragdoll" })}");

                    return new string[] { "test ok" };
                }
            }

            return new string[] { GetUsage() };
        }


    }
}

