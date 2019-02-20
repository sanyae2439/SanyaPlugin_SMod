using Smod2.Commands;
using Smod2.API;
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
            return "SANYA <RELOAD/BLACKOUT/GEN <UNLOCK/OPEN/ACT>/EV/TESLA <I>/SHAKE>";
        }

        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if(args.Length > 0)
            {
                if (args[0] == "reload")
                {
                    //plugin.ReloadConfig();

                    return new string[] { "failed. this command is outdated." };
                }
                else if (args[0] == "blackout")
                {
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
                        else if(args[1] == "act")
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
                    foreach(Elevator ev in plugin.Server.Map.GetElevators())
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
                else if (args[0] == "nuke")
                {
                    if (plugin.nuketest)
                    {
                        plugin.nuketest = false;
                    }
                    else
                    {
                        plugin.nuketest = true;
                    }
                    

                    return new string[] { "nuketest:" + plugin.nuketest };
                }
            }

            return new string[] { "no parameters." };
        }
    }
}