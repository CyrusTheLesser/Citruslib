using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Epic.OnlineServices;
using HarmonyLib;
using Landfall.Network;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;


namespace CitrusLib
{
    

    //holds miscelaneous functions typically internal to citruslib
    public static partial class Citrus
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

        public static NetworkDriver Network;

       //static BidirectionalDictionary<byte, NetworkConnection> connDict; //advanced!

        public static Queue<byte> kicklist = new Queue<byte>();


        public static List<PlayerRef> players = new List<PlayerRef>();

        public static List<PlayerTeam> teams = new List<PlayerTeam>();


        public static List<UnityTransportServer.BufferedPackage> queue = new List<UnityTransportServer.BufferedPackage>();

        //send nukes
        public static Dictionary<byte, Queue<UnityTransportServer.BufferedPackage>> buffQueue = new Dictionary<byte, Queue<UnityTransportServer.BufferedPackage>>();


        /// <summary>
        /// Performs an action once the player is alive and sending position updates again. The action is performed immediately if the player is currently alive.
        /// 
        /// good uses of this are team changes and gear changes, as the player should be alive for those
        /// </summary>
        /// <param name="action">The action to perform</param>
        public static void DoOnceAlive(PlayerRef p, Action action)
        {
            WaitUntil(() => { return (bool)(p.data["aliveAware"]); },action, 0.1f);
        }

        //TABGPlayerServer version
        public static void DoOnceAlive(TABGPlayerServer p, Action action)
        {
            PlayerRef pr = players.Find(prf => prf.player == p);
            if (pr == null)
            {
                log.LogError("Player wasnt found! Action Cancelled!");
                return;
            }
            DoOnceAlive(pr, action);
        }

        /// <summary>
        /// performs an action after a set amount of time
        /// </summary>
        /// <param name="time"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static async void WaitThen(float time, Action a)
        {
            await Task.Delay((int)(time * 1000));
            a();
        }

        /// <summary>
        /// performs an action once a condition is true
        /// </summary>
        /// <param name="condition">the condition to be met</param>
        /// <param name="a">the action to perform</param>
        /// <param name="tick">how often to check the condition, in seconds. dont set very low unless your condition is performant!</param>
        /// <returns></returns>

        internal static async void WaitUntil(Func<bool> condition, Action a, float tick = 0.2f)
        {
            while (!condition.Invoke())
            {
                if (tick <= 0)
                {
                    await Task.Yield();
                }
                else
                {
                    await Task.Delay((int)(tick * 1000));
                }

            }
            a();
            
        }


        /// <summary>
        /// performs an action after a set amount of time, as long as the condition remains true throughout the duration. can include a fail action as well
        /// </summary>
        /// <param name="time">time to wait before the succeed action</param>
        /// <param name="condition">the condition to check</param>
        /// <param name="a">the action to perform if the test succeeds</param>
        /// <param name="tick">how often to check the condition</param>
        /// <param name="fail">the action to invoke if the condition ever becomes false</param>
        /// <returns></returns>
        public static async void WaitThenAsLongAs(float time, Func<bool> condition, Action a, Action fail, float tick = 0.2f)
        {
            float timeStart = Time.timeSinceLevelLoad;
            while(Time.timeSinceLevelLoad < timeStart+time)
            {
                if (!condition.Invoke())
                {
                    if (fail != null)
                    {
                        fail();
                    }
                    return;
                }

                if (tick <= 0)
                {
                    await Task.Yield();
                }
                else
                {
                    await Task.Delay((int)(tick * 1000));
                }

            }
            if (a != null)
            {
                a();
            }
            
        }



        //tries to fix the buffer and tries to prevent buffered-buffered messages from going to the back of the queue
        /*public static void FixQueue(ref Queue<UnityTransportServer.BufferedPackage> buffPacks)
        {
            

            while (buffPacks.Count != 0)
            {
                UnityTransportServer.BufferedPackage buff = buffPacks.Dequeue();


                if(buff.RecipentIndicies.Length == 1 && buff.RecipentIndicies[0] == 255)
                {
                    List<byte> players = new List<byte>();

                    foreach(TABGPlayerServer player in World.GameRoomReference.Players)
                    {
                        players.Add(player.PlayerIndex);
                    }

                    buff.RecipentIndicies = players.ToArray();
                }

                foreach(byte b in buff.RecipentIndicies)
                {
                    UnityTransportServer.BufferedPackage newBuff = new UnityTransportServer.BufferedPackage();

                    newBuff.Data = buff.Data;
                    newBuff.EventCode = buff.EventCode;
                    newBuff.RecipentIndicies = new byte[]
                    {
                        b
                    };
                    
                    queue.Add(newBuff);
                }


            }

            
        }*/




        /// <summary>
        /// searches for a player first by their username, and then by index using the inputted string. good for chat commands
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool PlayerChatSearch(string name, out TABGPlayerServer result)
        {
            result = null;
            if(PlayerWithName(name, out result))
            {
                return true;
            }
            byte ind;
            if (byte.TryParse(name,out ind))
            {
                return PlayerWithIndex(ind, out result);
            }
            return false;
        }


        public static bool PlayerWithIndex(byte index, out TABGPlayerServer result)
        {
            result = null;

            List<TABGPlayerServer> players = World.GameRoomReference.Players
                .Where(x => x != null)
                .Where(p => p.PlayerIndex == index)
                .ToList();

            if (players.Count != 0)
            {
                result = players.First();
            }


            return players.Count == 1;
        }


        
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

    

        



        


        /// <summary>
        /// gets a random point in the current Ring's white circle, on the ground. Allows the use of a predicate for more specific conditions
        /// </summary>
        /// <param name="tries">the amount of times to try to find a valid point.</param>
        /// <param name="func">a predicate function to further evaluate the sampled position</param>
        /// <returns></returns>
        public static Vector3 RandomInRing(int tries = 10, Predicate<Vector3> func = null)
        {
            Vector3 pos = World.SpawnedRing.currentWhiteRingPosition;

            return RandomInCircle(pos, World.SpawnedRing.currentWhiteSize,tries, func);

        }

        /// <summary>
        /// gets a random point on land within the specified radius. Allows the use of a predicate for more specific conditions
        /// </summary>
        /// <param name="pos">the center of the search circle</param>
        /// <param name="radius">the radius of the search circle</param>
        /// <param name="tries">amount of times to try to find a point</param>
        /// <param name="func">a predicate function for further validating a point</param>
        /// <param name="initTries">for internal function use for debugging.</param>
        /// <returns></returns>
        public static Vector3 RandomInCircle(Vector3 pos, float radius, int tries = 10, Predicate<Vector3> func = null, int initTries=-1)
        {
            if (initTries == -1)
            {
                initTries = tries;
            }

            pos += UnityEngine.Random.insideUnitSphere * radius * 0.45f;

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
            if (tries > 0)
            {
                return RandomInCircle(pos, radius, tries, func,initTries);
            }
            pos.y = 200;
            log.LogError(string.Format("Random In circle failed after {0} tries!",initTries));
            return pos;
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
            data.Add("aliveAware",!player.IsDead);
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

        

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
           
            return base.GetHashCode();
        }


    }



}
