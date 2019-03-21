using Smod2.Commands;
using Smod2.API;
using System.Collections.Generic;
using UnityEngine;

namespace SanyaPlugin
{
    class CommandHandler : ICommandHandler
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
            return "SANYA <RELOAD/BLACKOUT/GEN <UNLOCK/OPEN/ACT>/EV/TESLA \\<I\\>/SHAKE>";
        }

        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "reload")
                {
                    //plugin.ReloadConfig();

                    return new string[] { "failed. this command is outdated." };
                }
                else if (args[0] == "blackout")
                {
                    List<Room> rooms = new List<Room>(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA)).FindAll(x => x.ZoneType == ZoneType.LCZ);
                    foreach (Room r in rooms)
                    {
                        r.FlickerLights();
                    }
                    Generator079.mainGenerator.CallRpcOvercharge();

                    return new string[] { "blackout success." };
                }
                else if (args[0] == "gen")
                {
                    if (args.Length > 1)
                    {
                        if (args[1] == "unlock")
                        {
                            foreach (Generator items in plugin.Server.Map.GetGenerators())
                            {
                                items.Unlock();
                            }
                            return new string[] { "gen unlock." };
                        }
                        else if (args[1] == "open")
                        {
                            foreach (Generator items in plugin.Server.Map.GetGenerators())
                            {
                                items.Open = true;
                            }
                            return new string[] { "gen open." };
                        }
                        else if (args[1] == "act")
                        {
                            foreach (Generator items in plugin.Server.Map.GetGenerators())
                            {
                                if (!items.Engaged)
                                {
                                    items.TimeLeft = 1.0f;
                                    items.HasTablet = true;
                                }
                            }
                            return new string[] { "gen activate." };
                        }
                    }
                }
                else if (args[0] == "ev")
                {
                    foreach (Elevator ev in plugin.Server.Map.GetElevators())
                    {
                        ev.Use();
                    }
                    return new string[] { "EV used." };
                }
                else if (args[0] == "tesla")
                {
                    bool isInstant = false;

                    if (args.Length > 1)
                    {
                        if (args[1] == "i")
                        {
                            isInstant = true;
                        }
                    }

                    foreach (Smod2.API.TeslaGate tesla in plugin.Server.Map.GetTeslaGates())
                    {
                        tesla.Activate(isInstant);
                    }
                    return new string[] { "tesla activated." };
                }
                else if (args[0] == "shake")
                {
                    plugin.Server.Map.Shake();

                    return new string[] { "map shaking." };
                }
                else if (args[0] == "heli")
                {
                    SanyaPlugin.CallVehicle(false);

                    return new string[] { "heli moved." };
                }
                else if (args[0] == "van")
                {
                    SanyaPlugin.CallVehicle(true);

                    return new string[] { "van spawned." };
                }
                else if (args[0] == "flagtest")
                {
                    if (plugin.test)
                    {
                        plugin.test = false;
                    }
                    else
                    {
                        plugin.test = true;
                    }

                    return new string[] { $"test:{plugin.test}" };
                }
                else if (args[0] == "test")
                {
                    Player ply = sender as Player;
                    System.Random rnd = new System.Random();
                    GameObject gameObject = null;

                    if (ply != null)
                    {
                        gameObject = ply.GetGameObject() as GameObject;
                    }

                    

                    //foreach (Camera079 item in Scp079PlayerScript.allCameras)
                    //{
                    //    plugin.Debug($"Name:{item.cameraName}");
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

                    if(gameObject != null)
                    {
                        foreach(var i in UnityEngine.Object.FindObjectsOfType<FallDamage>())
                        {
                            plugin.Debug($"zone:{i.zone} {i.name}");
                        }
                    }

                    return new string[] { "test ok" };
                }
            }

            return new string[] { GetUsage() };
        }
    }
}

