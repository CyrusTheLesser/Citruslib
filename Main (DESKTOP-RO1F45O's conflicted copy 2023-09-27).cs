using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Landfall.Network;
using UnityEngine;


namespace Citruslib
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "citrusbird.tabg.citruslib";
        public const string pluginName = "Citrus Lib";
        public const string pluginVersion = "0.1";

        public void Awake()
        {
            Console.WriteLine("Citrus Library Loading!");
            LandLog.Log("Citrus Library Loading!");

            Harmony harmony = new Harmony(pluginGuid);


            harmony.PatchAll();


            CitrusLib.AddCommand("team",delegate(string[] prms, TABGPlayerServer player)
            {
                switch (prms[0])
                {
                    case "get":
                        if (prms.Length != 2)
                        {
                            CitrusLib.SelfParrot(player, "team get <name>");
                            return;
                        }
                        TABGPlayerServer find = null;
                        if (!CitrusLib.PlayerWithName(prms[1], out find))
                        {
                            if (find != null)
                            {
                                CitrusLib.SelfParrot(player, "multiple results for: " + prms[1]);
                            }
                            else
                            {
                                CitrusLib.SelfParrot(player, "no results for: " + prms[1]);
                            }
                            return;
                        }

                        CitrusLib.SelfParrot(player, "groupIndex for player " + find.PlayerName + " is:" + find.GroupIndex);

                        break;
                    case "set":
                        if (prms.Length != 3)
                        {
                            CitrusLib.SelfParrot(player, "team set <name> <index>");
                            return;
                        }
                        TABGPlayerServer setFind = null;
                        if (!CitrusLib.PlayerWithName(prms[1], out setFind))
                        {
                            if (setFind != null)
                            {
                                CitrusLib.SelfParrot(player, "multiple results for: " + prms[1]);
                            }
                            else
                            {
                                CitrusLib.SelfParrot(player, "no results for: " + prms[1]);
                            }
                            return;
                        }
                        byte ind;
                        if(!byte.TryParse(prms[2],out ind))
                        {
                            CitrusLib.SelfParrot(player, "could not parse group index: " + prms[2]);
                            return;
                        }

                        CitrusLib.SetTeam(setFind, ind);
                        CitrusLib.SelfParrot(player, "set player " + setFind.PlayerName + " to team " + prms[2]);
                        break;
                    default:
                        CitrusLib.SelfParrot(player,"unknown parameter: " + prms[0]);
                        break;
                }
            });



            CitrusLib.AddCommand("goto", delegate (string[] prms, TABGPlayerServer player)
            {
                TABGPlayerServer find = null;
                if (prms.Length != 1)
                {
                    CitrusLib.SelfParrot(player, "goto <name>");
                    return;
                }
                if (!CitrusLib.PlayerWithName(prms[0], out find))
                {
                    if (find != null)
                    {
                        CitrusLib.SelfParrot(player, "multiple results for: " + prms[0]);
                    }
                    else
                    {
                        CitrusLib.SelfParrot(player, "no results for: " + prms[0]);
                    }
                    return;
                }
                LandLog.Log(string.Format("taking player {0} to {1}",player.PlayerName, find.PlayerName));
                CitrusLib.Teleport(player, find.PlayerPosition);

            });

            CitrusLib.AddCommand("bring", delegate (string[] prms, TABGPlayerServer player)
            {
                TABGPlayerServer find = null;
                if (prms.Length != 1)
                {
                    CitrusLib.SelfParrot(player, "bring <name>");
                    return;
                }
                if (!CitrusLib.PlayerWithName(prms[0], out find))
                {
                    if (find != null)
                    {
                        CitrusLib.SelfParrot(player, "multiple results for: " + prms[0]);
                    }
                    else
                    {
                        CitrusLib.SelfParrot(player, "no results for: " + prms[0]);
                    }
                    return;
                }
                LandLog.Log(string.Format("taking player {0} to {1}", find.PlayerName, player.PlayerName));
                CitrusLib.Teleport(find, player.PlayerPosition);

            });

            CitrusLib.AddCommand("start", delegate (string[] prms, TABGPlayerServer player)
             {
                 float time = 30;
                 if (prms.Length > 0)
                 {
                     if (prms.Length != 1)
                     {
                         CitrusLib.SelfParrot(player, "start <time(optional>");
                         return;
                     }
                     if (!float.TryParse(prms[0],out time)){
                         CitrusLib.SelfParrot(player, "invalid time: " + prms[0]);
                         return;
                     }
                         
                     
                 }
                 CitrusLib.World.GameRoomReference.StartCountDown(time);
             });

        }
        

    }

    //prevents players from setting their gear every time they spawn. allows opposing-clientsided gear changing
    [HarmonyPatch(typeof(GearChangeCommand),nameof(GearChangeCommand.Run))]
    class GearPatch
    {
        static bool Prefix(byte[] msgData, ServerClient world)
        {
            byte index;
            int[] array;
            using (MemoryStream memoryStream = new MemoryStream(msgData))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    index = binaryReader.ReadByte();
                    /*
                    int num = binaryReader.ReadInt32();
                    array = new int[num];
                    for (int i = 0; i < num; i++)
                    {
                        array[i] = binaryReader.ReadInt32();
                    }*/
                }
            }
            TABGPlayerServer tabgplayerServer = world.GameRoomReference.FindPlayer(index);
            if (tabgplayerServer == null || tabgplayerServer.GearData.Length!=0)
            {
                return false;
            }
            return true;

        }
    }


    //creates player references and team references
    [HarmonyPatch(typeof(GameRoom), nameof(GameRoom.AddPlayer))]
    class AddPlayerPatch
    {
        static void Postfix(TABGPlayerServer p, bool wantsToBeAlone)
        {
            


            PlayerRef pRef = new PlayerRef(p);

            if(CitrusLib.players == null)
            {
                LandLog.Log("player list is null?");
                CitrusLib.players = new List<PlayerRef>();
            }

            CitrusLib.players.Add(pRef);

            PlayerTeam myTeam = CitrusLib.teams.Find(t => t.groupIndex == p.GroupIndex);

            if(myTeam == null)
            {
                myTeam = new PlayerTeam(p.GroupIndex);
                CitrusLib.teams.Add(myTeam);
            }
            myTeam.players.Add(pRef);
            //im pretty confused at this point
            //Citruslib.CitrusLib.AllyTo(p, CitrusLib.World.GameRoomReference.CurrentGameStats.GetTeam(p.GroupIndex).PlayersInTeam[0]);

            //for now im not doing that as, also for now, ill just test with games starting as solos only so this issue shouldnt be present...

        }

    }

    //removes player (but not team reference)
    [HarmonyPatch(typeof(GameRoom), nameof(GameRoom.RemovePlayer))]
    class RemovePlayerPatch
    {
        static void Prefix(TABGPlayerServer p)
        {
            if (p == null)
            {
                return;
            }

            PlayerRef player = CitrusLib.players.Find(pl => pl.player == p);

            if (player == null) return;

            PlayerTeam team = CitrusLib.teams.Find(t => t.players.Contains(player));
            

            if (team != null)
            {
                team.players.Remove(player);
                return;
            }
            CitrusLib.players.Remove(player);
            
            



            //going a little bit insane

        }

    }



    [HarmonyPatch(typeof(ChatMessageCommand), nameof(ChatMessageCommand.Run))]
    class ChatPatch
    {
        static bool Prefix(byte[] msgData, ServerClient world, byte sender)
        {
            
            TABGPlayerServer player = CitrusLib.World.GameRoomReference.FindPlayer(sender);
            if(player == null)
            {
                return false;
            }
            

            using (MemoryStream memoryStream = new MemoryStream(msgData))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    byte index = binaryReader.ReadByte();
                    byte count = binaryReader.ReadByte();
                    string message = Encoding.Unicode.GetString(binaryReader.ReadBytes((int)count));

                    string[] prms = message.Split(' ');

                    int i = 0;
                    foreach(string s in prms)
                    {
                        prms[i] = s.ToLower();
                        i++;
                    }


                    if(prms[0].StartsWith("/"))
                    {
                        //runs command and doesnt say it in chat
                        List<string> prmsReal = prms.ToList();
                        prmsReal.RemoveAt(0);
                        CitrusLib.RunCommand(prms[0].Replace("/",""), prmsReal.ToArray(), player);
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
