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
    public static class CitrusLib
    {
        static ServerClient currentWorld = null;

        public static ServerClient World
        {
            get
            {
                if (currentWorld != null)
                {
                    return currentWorld;
                }
                else
                {
                    currentWorld = UnityEngine.Object.FindObjectOfType<ServerClient>();
                    return currentWorld;
                }
            }
        }

        public static List<PlayerRef> players = new List<PlayerRef>();

        public static List<PlayerTeam> teams = new List<PlayerTeam>();

        static Dictionary<string, Command>  commands = new Dictionary<string, Command>();




        //gets the name of a player who's name starts with the provided string. returns false if none or more than one found
        public static bool PlayerWithName(string name,out TABGPlayerServer result)
        {
            result = null;

            List<TABGPlayerServer> players = World.GameRoomReference.Players
                .Where(x => x != null)
                .Where(p => p.PlayerName.ToLower().StartsWith(name))
                .ToList();

            if (players.Count != 0)
            {
                result = players.First();
            }

            

            return players.Count ==1;
        }

        public static void RunCommand(string name, string[] prms,TABGPlayerServer player)
        {
            Command cmd = null;
            if(commands.TryGetValue(name, out cmd))
            {
                cmd.Run(prms, player);
               
            }
            else
            {
                SelfParrot(player, "unknown command: "+name);
                
            }
        }

        
        //not the best implementation as it doesnt allow mass-editing the function... but whos going around editing the commands anyways...
        public static void AddCommand(string[] names, Action<string[], TABGPlayerServer> function, bool adminOnly = true)
        {
            foreach(string name in names)
            {
                AddCommand(name, function, adminOnly);
            }
        }

        public static void AddCommand(string name, Action<string[], TABGPlayerServer> function, bool adminOnly = true)
        {
            if (commands.ContainsKey(name))
            {
                LandLog.Log("command " + name + " already registered by another mod!");
                return;
            }
            if(function == null)
            {
                LandLog.Log("Refusing to register command "+name+" as it has no function!");
                return;
            }
            commands.Add(name,new Command(name, function, adminOnly));


        }


        //numbers over 256 in a stack are meant to be supported
        public static void GiveLoot(TABGPlayerServer player, int item, int quantity = 1)
        {
            int[] weps = new int[]
            {
                item
            };

            List<byte> quants = new List<byte>();

            while (quantity > 0)
            {
                quants.Add((byte)Math.Min(quantity, 254));
                quantity -= 254;
            }


            GivePickUpCommand.Run(null, World, player.PlayerIndex, weps, quants.ToArray());
        }

        //ideally, moves the player to the desired location without removing gear or changing health
        public static void Teleport(TABGPlayerServer player, Vector3 pos)
        {
            RespawnEntityCommand.Run(World, player, pos, byte.MaxValue);
        }


        //gets a random point inside the current circle, on the ground
        //allows using predicate to continue trying if position isnt valid based off arbitrary criteria
        public static Vector3 RandomInCircle(int tries = 10, Predicate<Vector3> func = null)
        {
            Vector3 pos = World.SpawnedRing.currentWhiteRingPosition;

            pos += UnityEngine.Random.insideUnitSphere * World.SpawnedRing.currentWhiteSize*0.45f;

            pos.y = 1500;

            Vector3 result = Vector3.zero;
            RaycastHit raycastHit;
            Physics.Raycast(new Ray(pos, Vector3.down), out raycastHit, 3000f, (LayerMask.NameToLayer("Map") | LayerMask.NameToLayer("Terrain")));

            if (raycastHit.transform && raycastHit.point.y > 140f)
            {
                result = raycastHit.point;
            }



            if (result != Vector3.zero)
            {
                if (func == null)
                {
                    return result;
                }

                if (func(result))
                {
                    return result;
                }
                
            }
            tries--;
            if(tries > 0)
            {
                return RandomInCircle(tries, func);
            }
            pos.y = 200;
            return pos;

        }

        //creates a login buffer with a new teamindex for changing teams. for lying to clients
        static byte[] LoginData(TABGPlayerServer ply, byte newGroupIndex)
        {

            byte[] bytes = Encoding.UTF8.GetBytes(ply.PlayerName);
            byte[] buffer = new byte[15 + bytes.Length + 4 * ply.GearData.Length];
            using (MemoryStream memoryStream2 = new MemoryStream(buffer))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream2))
                {
                    binaryWriter.Write(ply.PlayerIndex);
                    binaryWriter.Write(newGroupIndex);
                    binaryWriter.Write(bytes.Length);//name length
                    binaryWriter.Write(bytes);//name bytes
                    binaryWriter.Write(ply.GearData.Length); //gear length
                    for (int j = 0; j < ply.GearData.Length; j++)
                    {
                        binaryWriter.Write(ply.GearData[j]);//individual gear ints
                    }
                    binaryWriter.Write(false); //i forgor but i dont think it matters
                    binaryWriter.Write((int)0); //player color. unused in battleroyale
                }
            }
            return buffer;
        }

        static void RebuildStandings()
        {
            GameStats stats = World.GameRoomReference.CurrentGameStats;
            stats.Reset();

            foreach(TABGPlayerServer player in World.GameRoomReference.Players)
            {
                stats.AddPlayerToTeam(player.GroupIndex, player, true);
            }

            foreach(TeamStanding team in stats.GetAllTeams())
            {
                team.InitNumberOfLives(2147483647);
            }
        }

        //'vanilla' parameters
        public static void SetTeam(TABGPlayerServer p, byte groupIndex)
        {
            PlayerRef player = players.Find(pl => pl.player == p);
            if (player == null)
            {
                LandLog.Log("player ref not found...");
                return;
            }
            PlayerTeam teamto = teams.Find(t => t.groupIndex == groupIndex);

            if(teamto == null)
            {
                LandLog.Log("team not found");
                return;
            }
            SetTeam(player, teamto); //oh

        }

        public static void SetTeam(PlayerRef player, PlayerTeam team)
        {
            if (team != null)
            {
                if (team.players.Contains(player))
                {
                    LandLog.Log("already on team " +team.groupIndex+", returning");
                    return; //already on team, shouldnt waste bandwidth
                }
            }
         
            //remove existing alliances
            PlayerTeam currentTeam = teams.Find(t => t.players.Contains(player));
            if (currentTeam != null)
            {
                currentTeam.players = currentTeam.players.Distinct().ToList();
                currentTeam.players.Remove(player);
            }
           
            //add new* alliances
            if (team != null)
            {
                team.players = team.players.Distinct().ToList();
                team.players.Add(player);
                player.player.UpdateGroupIndex(team.groupIndex); //hellish consequences
             
            }
            else
            {
                LandLog.Log("setting null team can have issues if the players original GI is a still-existing team");
                player.player.UpdateGroupIndex(player.Get<byte>("originalTeam"));
            }
        
            //might not work....
            foreach(PlayerRef pl in players)
            {
                if (pl == player) continue;
                SetAlly(player, pl, player.player.GroupIndex == pl.player.GroupIndex);
            }


            RebuildStandings();
        
        }

        public static void OldSetTeam(PlayerRef player, PlayerTeam team)
        {
            if (team != null)
            {
                if (team.players.Contains(player))
                {
                    LandLog.Log("already on team " + team.groupIndex + ", returning");
                    return; //already on team, shouldnt waste bandwidth
                }
            }

            //remove existing alliances
            PlayerTeam currentTeam = teams.Find(t => t.players.Contains(player));
            if (currentTeam != null)
            {
                currentTeam.players = currentTeam.players.Distinct().ToList();
                foreach (PlayerRef pr in currentTeam.players)
                {
                    if (pr == player) continue;
                    SetAlly(player, pr, false);
                }
                currentTeam.players.Remove(player);
            }

            //add new* alliances
            if (team != null)
            {
                team.players = team.players.Distinct().ToList();
                foreach (PlayerRef pr in team.players)
                {
                    if (pr == player) continue;
                    SetAlly(player, pr, true);
                }

                team.players.Add(player);

                player.player.UpdateGroupIndex(team.groupIndex); //hellish consequences

            }
            else
            {
                LandLog.Log("setting null team can have issues if the players original GI is a still-existing team");
                player.player.UpdateGroupIndex(player.Get<byte>("originalTeam"));
            }


            RebuildStandings();

        }

        //if onTeam is true, sets players to be allies with one another. otherwise, sets them to NOT be allies with one another...
        static void SetAlly(PlayerRef player,PlayerRef ally,bool onTeam)
        {
            

            byte plIndex = player.player.PlayerIndex;
            byte index = ally.player.PlayerIndex;
            LandLog.Log(string.Format("Setting player {0} and {1} to be {2}!",plIndex,index,onTeam ? "friends" : "enemies"));
            byte pgi = player.Get<byte>("originalTeam");
            byte agi = ally.Get<byte>("originalTeam");

            int t = onTeam ? 0 : 1;

            //player leaves ally's game
            World.SendMessageToClients(EventCode.PlayerLeft, new byte[] { plIndex, (byte)1 }, index, true);
            //ally leaves player's game
            World.SendMessageToClients(EventCode.PlayerLeft, new byte[] { index, (byte)1 }, plIndex, true);
            
            
            //player joins ally's game with ally's GI or GI+1 if enemy
            World.SendMessageToClients(EventCode.Login, LoginData(player.player, (byte)((int)agi + t)), index, true);
            //ally joins player's game with player's groupindex, or GI+1 if enemy
            World.SendMessageToClients(EventCode.Login, LoginData(ally.player, (byte)((int)pgi + t)), plIndex, true);

        }

         

        //sends a private parrot message to the player. hopefully sends the parrot straight downwards
        public static void SelfParrot(TABGPlayerServer p,string message)
        {
            p.UpdateRotation(new Vector2(90, 90));

            byte[] bytes = Encoding.Unicode.GetBytes(message);
            byte[] array = new byte[2 + bytes.Length];
            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(p.PlayerIndex);
                    binaryWriter.Write((byte)bytes.Length);
                    binaryWriter.Write(bytes);
                }
            }
            //might work

            int num = array.Length - 1;
            byte[] array2 = new byte[num];
            Array.Copy(array, 1, array2, 0, num);
            byte[] buffer = new byte[22 + array2.Length];
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(p.PlayerIndex);
                    binaryWriter.Write(p.PlayerPosition.x);
                    binaryWriter.Write(p.PlayerPosition.y);
                    binaryWriter.Write(p.PlayerPosition.z);
                    binaryWriter.Write(p.PlayerRotation.x);
                    binaryWriter.Write(p.PlayerRotation.y);
                    binaryWriter.Write(array2);
                }
            }
            World.SendMessageToClients(EventCode.ThrowChatMessage, buffer, p.PlayerIndex, true, false);
        }

        //sets player's gear
        public static void SetGear(TABGPlayerServer player, int[] gearData)
        {

        }


    }

    

    class Command
    {
        string name;
        public Action<string[], TABGPlayerServer> func;
        bool adminonly;

        public Command(string n, Action<string[], TABGPlayerServer> f, bool a)
        {
            name = n;
            func = f;
            adminonly = a;
        }

        public void Run(string[] prms, TABGPlayerServer player)
        {
            if (player == null)
            {
                return;
            }
            if (adminonly & !player.IsAdmin)
            {
                LandLog.Log(string.Format("Player {0} tried to use command {1} but isnt admin!", player.PlayerName, name));
                CitrusLib.SelfParrot(player, "Command \"" + name + "\" is admin only");
                return;
            }
            //runs the function!
            func(prms, player);
        }
    }

    public class PlayerTeam
    {
        public List<PlayerRef> players = new List<PlayerRef>();
        public byte groupIndex;

        public PlayerTeam(byte ind)
        {
            players = new List<PlayerRef>();
            groupIndex = ind;
        }
    }

    //should be useable by other mods to store data about the player
    public class PlayerRef
    {
        public Dictionary<string, object> data;

        public TABGPlayerServer player;



        public PlayerRef(TABGPlayerServer player)
        {
            data = new Dictionary<string, object>();
            this.player = player;
            data.Add("originalTeam", player.GroupIndex);
            //data["originalTeam"] = player.GroupIndex;
            //data["allies"] = new List<PlayerRef>();
            //data.Add("allies", new List<PlayerRef>());
        }

        public T Get<T>(string key)
        {
            if (data.TryGetValue(key, out object ret))
            {
                return (T)ret;
            }
            else
            {
                return default;
            }

        }

        // override object.Equals for equality comparison for stuff like List.Distinct to consider matching-index players to be the same. because they are.
        public override bool Equals(object obj)
        {
            

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return ((PlayerRef)obj).player.PlayerIndex == this.player.PlayerIndex;

        }

        public static explicit operator TABGPlayerServer(PlayerRef pref)
        {
            if (pref == null) return null;
            return pref.player;
        }


    }

}
