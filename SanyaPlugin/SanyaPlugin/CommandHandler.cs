using Smod2.Commands;
using Smod2.API;
using System;
using System.Collections.Generic;

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
            return "SANYA <BLACKOUT>";
        }

        public string[] OnCall(ICommandSender sender, string[] args)
        {
            if(args.Length > 0)
            {
                if (args[0] == "blackout")
                {
                    Generator079.mainGenerator.CallRpcOvercharge();

                    return new string[] { "blackout success." };
                }else if(args[0] == "l")
                {
                    List<Room> rooms = new List<Room>();
                    rooms.AddRange(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA));

                    rooms = rooms.FindAll(items => { return items.ZoneType == ZoneType.LCZ; });

                    rooms.ForEach(items => { items.FlickerLights(); plugin.Info(items.RoomType.ToString()); });

                    return new string[] { rooms.Count.ToString() };
                }else if (args[0] == "ls")
                {
                    List<Room> rooms = new List<Room>();
                    rooms.AddRange(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.SPEAKER));

                    rooms = rooms.FindAll(items => { return items.ZoneType == ZoneType.LCZ; });

                    rooms.ForEach(items => { items.FlickerLights(); plugin.Info(items.RoomType.ToString()); });

                    return new string[] { rooms.Count.ToString() };
                }
                else if (args[0] == "h")
                {
                    List<Room> rooms = new List<Room>();
                    rooms.AddRange(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.CAMERA));

                    rooms = rooms.FindAll(items => { return items.ZoneType == ZoneType.HCZ; });

                    rooms.ForEach(items => { items.FlickerLights(); plugin.Info(items.RoomType.ToString()); });

                    return new string[] { rooms.Count.ToString() };
                }
                else if (args[0] == "hs")
                {
                    List<Room> rooms = new List<Room>();
                    rooms.AddRange(plugin.Server.Map.Get079InteractionRooms(Scp079InteractionType.SPEAKER));

                    rooms = rooms.FindAll(items => { return items.ZoneType == ZoneType.HCZ; });

                    rooms.ForEach(items => { items.FlickerLights(); plugin.Info(items.RoomType.ToString()); });

                    return new string[] { rooms.Count.ToString() };
                }
                else if (args[0] == "g")
                {
                    Generator[] gens = plugin.Server.Map.GetGenerators();

                    foreach(Generator items in gens)
                    {
                        plugin.Debug(items.Room.RoomType.ToString());
                        items.Unlock();
                    }

                    return new string[] { "ok g" };
                }else if(args[0] == "reload")
                {
                    plugin.ReloadConfig();

                    return new string[] { "reload ok" };
                }
            }

            return new string[] { "no parameters." };
        }
    }
}