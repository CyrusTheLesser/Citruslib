using Landfall.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Epic.OnlineServices.AntiCheatCommon;

namespace CitrusLib
{
    //do stuff to players
    public static partial class Citrus
    {



        
        public static void Teleport(TABGPlayerServer player, Vector3 pos, bool keepLoot = true)
        {
            Teleport(player, pos, player.PlayerRotation, keepLoot);
        }

        /// <summary>
        /// teleports a living player. if the player is dead, they are respawned.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="keepLoot"></param>
        public static void Teleport(TABGPlayerServer player, Vector3 pos, Vector2 rot, bool keepLoot = true)
        {
            byte[] array = new byte[(int)(24)];
            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write((byte)1);
                    binaryWriter.Write(player.PlayerIndex);
                    binaryWriter.Write(player.PlayerIndex);
                    binaryWriter.Write(player.Health);
                    binaryWriter.Write(pos.x);
                    binaryWriter.Write(pos.y);
                    binaryWriter.Write(pos.z);
                    binaryWriter.Write((float)rot.x);
                    binaryWriter.Write(byte.MaxValue);
                    //RespawnEntityCommand.Run(World, player, pos, player.PlayerIndex);
                }
            }

            LootPack lp = LootPack.CopyPlayerLoot(player.Loot);

            if (player.IsDead)
            {
                Respawn(player, pos, false);
            }
            else
            {
                player.ClearLoot();
                player.ClearEquipment();
                World.SendMessageToClients(EventCode.PlayerRespawn, array, player.PlayerIndex, true, false);
            }
            

            if (keepLoot)
            {
                lp.GiveTo(player);
            }

        }


     

        /// <summary>
        /// respawns the player using the vanilla method. Also works as a means to teleport players, reviving them if they are dead. can be configured to wait until the player's time scale returns to normal (about 7 seconds after death)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="pos"></param>
        /// <param name="waitUntilReady"></param>
        public static void Respawn(TABGPlayerServer player, Vector3 pos, bool waitUntilReady=false)
        {
            if (!waitUntilReady)
            {
                RespawnEntityCommand.Run(World, player, pos);
                return;
            }
            WaitThen(8,delegate()
            {
                RespawnEntityCommand.Run(World, player, pos);
            });


        }

        //kills a player and allow choosing who is awarded the kill
        public static void KillPlayer(TABGPlayerServer player, TABGPlayerServer killer = null)
        {
            if (player == null)
            {
                Citrus.log.LogError("Cant Find victim player that is dead");
                return;
            }
            player.Kill();
            player.TakeDamage(200f);

            if (killer != null)
            {
                killer.AddKill();
                byte groupIndex = killer.GroupIndex;
                Citrus.World.GameRoomReference.CurrentGameKills.AddKillForTeam(groupIndex);
            }

            //TODO: SEND MESSAGE

        }

        public static void SetLoot(TABGPlayerServer player, int[] items, int[] quantities)
        {
            SetLoot(player, items.ToList(), quantities.ToList());
        }

        public static void SetLoot(TABGPlayerServer player, List<int> items, List<int> quantities)
        {
            LootPack lp = new LootPack();
            for(int i =0; i<items.Count();i++)
            {
                lp.AddLoot(items[i], quantities[i]);
            }
            SetLoot(player, lp);
        }


        //removes the players loot and then gives them these items
        public static void SetLoot(TABGPlayerServer player, LootPack loot)
        {


            //do something to remove the player's loot, such as teleport with no keeploot
            Teleport(player, player.PlayerPosition, player.PlayerRotation, false);

            loot.GiveTo(player);

        }


        

        //USE THIS IF GIVING ONE ITEM!
        public static void GiveLoot(TABGPlayerServer player, int item, int quantity = 1)
        {

            LootPack lp = new LootPack();
            lp.AddLoot(item, quantity);

            //GivePickUpCommand.Run(null, Citrus.World, player.PlayerIndex, weps, quants.ToArray());
        }

        public static void GiveLoot(TABGPlayerServer player, LootPack lp)
        {
            lp.GiveTo(player);
        }




        //APPARENTLY kicks only work if you WAIT for the player to be connected before kicking them...
        public static void Kick(TABGPlayerServer player, KickReason reason = KickReason.Invalid, string logReason = "")
        {
            if (player == null)
            {
                Citrus.log.LogError("tried kicking null player?");
            }
            if (logReason != "")
            {
                Citrus.log.Log(string.Format("kicking player {0}, Ind:{1}, Epic:{2}, for reason: {3}", player.PlayerName, player.PlayerIndex, player.EpicUserName, logReason));
            }



            Citrus.World.SendMessageToClients(EventCode.KickPlayerMessage, new byte[] { (byte)Mathf.Clamp((int)reason+100,byte.MinValue, byte.MaxValue)}, player.PlayerIndex, true, false);
            //world.HandlePlayerLeave(player);
            //PlayerLeaveCommand.Run(world, player, true);

            //PlayerLeaveCommand.RemovePlayer(player, gameRoomReference);

            byte b = player.PlayerIndex;
            //int id = ((UnityTransportServer)(world.Server)).
            Citrus.World.SendMessageToClients(EventCode.PlayerLeft, new byte[]
            {
                player.PlayerIndex,
                1
            }, byte.MaxValue, true, false);

            Citrus.World.GameRoomReference.RemovePlayer(player);
            Citrus.World.GameRoomReference.CurrentGameMode.HandlePlayerLeave(player);

            Citrus.World.GameRoomReference.CheckGameState();
            Citrus.World.Server.DisconnectPlayer(b);
            //this all works well enough so far but 


            Citrus.log.Log("kicklist enqueue " + b);
            Citrus.kicklist.Enqueue(b);
            Citrus.World.Server.Update();//maybe???????
                                         //Network.Disconnect();


            /*Citruslib.CitrusLib.World.WaitThenDoAction(2f, delegate
             {
                 //world.SendMessageToClients(EventCode.KickPlayer, Array.Empty<byte>(), b, true, false);
                 world.Server.DisconnectPlayer(b);
             });*/





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
            GameStats stats = Citrus.World.GameRoomReference.CurrentGameStats;
            stats.Reset();

            foreach (TABGPlayerServer player in Citrus.World.GameRoomReference.Players)
            {
                stats.AddPlayerToTeam(player.GroupIndex, player, true);
            }

            foreach (TeamStanding team in stats.GetAllTeams())
            {
                team.InitNumberOfLives(2147483647);
            }
        }

        //'vanilla' parameters
        public static void SetTeam(TABGPlayerServer p, byte groupIndex)
        {
            PlayerRef player = Citrus.players.Find(pl => pl.player == p);
            if (player == null)
            {
                Citrus.log.LogError("player ref not found...");
                return;
            }
            PlayerTeam teamto = Citrus.teams.Find(t => t.groupIndex == groupIndex);

            if (teamto == null)
            {
                Citrus.log.Log("team not found");
                return;
            }
            SetTeam(player, teamto); //oh

        }





        public static void SetTeam(PlayerRef player, PlayerTeam team)
        {
            //players = players.Distinct().ToList();

            foreach (PlayerRef pref in Citrus.players)
            {
                Citrus.log.Log(pref.player.PlayerIndex + " : " + pref.player.PlayerName);
            }

            if (team != null)
            {
                if (team.players.Contains(player))
                {
                    Citrus.log.Log("already on team " + team.groupIndex + ", returning");
                    return; //already on team, shouldnt waste bandwidth
                }
            }

            //remove existing alliances
            PlayerTeam currentTeam = Citrus.teams.Find(t => t.players.Contains(player));
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
                Citrus.log.Log("setting null team can have issues if the players original GI is a still-existing team");
                player.player.UpdateGroupIndex(player.Get<byte>("originalTeam"));
            }

            //might not work....
            foreach (PlayerRef pl in Citrus.players)
            {
                if (pl == player) continue;
                SetAlly(player, pl, player.player.GroupIndex == pl.player.GroupIndex);
            }


            RebuildStandings();

        }


        //if onTeam is true, sets players to be allies with one another. otherwise, sets them to NOT be allies with one another...
        static void SetAlly(PlayerRef player, PlayerRef ally, bool onTeam)
        {


            byte plIndex = player.player.PlayerIndex;
            byte index = ally.player.PlayerIndex;
            Citrus.log.Log(string.Format("Setting player {0} and {1} to be {2}!", plIndex, index, onTeam ? "friends" : "enemies"));
            byte pgi = player.Get<byte>("originalTeam");
            byte agi = ally.Get<byte>("originalTeam");

            int t = onTeam ? 0 : 1;

            //player leaves ally's game
            Citrus.World.SendMessageToClients(EventCode.PlayerLeft, new byte[] { plIndex, (byte)1 }, index, true);
            //ally leaves player's game
            Citrus.World.SendMessageToClients(EventCode.PlayerLeft, new byte[] { index, (byte)1 }, plIndex, true);


            //player joins ally's game with ally's GI or GI+1 if enemy
            Citrus.World.SendMessageToClients(EventCode.Login, LoginData(player.player, (byte)((int)agi + t)), index, true);
            //ally joins player's game with player's groupindex, or GI+1 if enemy
            Citrus.World.SendMessageToClients(EventCode.Login, LoginData(ally.player, (byte)((int)pgi + t)), plIndex, true);

        }



        //sends a private parrot message to the player. hopefully sends the parrot straight downwards
        public static void SelfParrot(TABGPlayerServer p, string message)
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
            Citrus.World.SendMessageToClients(EventCode.ThrowChatMessage, buffer, p.PlayerIndex, true, false);
        }

        //makes the recipient see a message from the player. they are the only one who sees the message!
        public static void Whisper(TABGPlayerServer player, TABGPlayerServer recipt,string message)
        {

        }

        //sets player's gear
        public static void SetGear(TABGPlayerServer player, int[] gearData)
        {

        }

    }



}
