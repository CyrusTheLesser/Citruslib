using Landfall.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Epic.OnlineServices.AntiCheatCommon;
using Newtonsoft.Json;

namespace CitrusLib
{
    public static partial class Citrus
    {
        internal static PermList permList;

        static SettingsFile<PermList> perms;


        static Dictionary<string, Command> commands = new Dictionary<string, Command>();


        internal static void WriteNewPermList()
        {
            perms.settings = new List<PermList>()
            {
                permList
            };


            perms.WriteSettings();


        }

        internal static void LoadPermSettings(string path)
        {
            bool absolute = true;
            if (path == "")
            {
                absolute = false;
                path = "PlayerPerms";
            }

            
                //make settings file
                perms = new SettingsFile<PermList>(path, absolute);
                //write ALL defaults before reading!!!

                PermList defaultPermList = new PermList();
                defaultPermList.name = "players";
                    defaultPermList.description = "List of players with modified permission level. defafult permission level is 0. commands can have different required permission levels, and the PL of players can even be lowered!";

                PermList.PermPlayer dp = new PermList.PermPlayer();
                dp.epic = "epic goes here";
                dp.name = "player name (does not need to be exact!)";
                dp.permlevel = 1;

                defaultPermList.players.Add(dp);
                defaultPermList.players.Add(dp);

                //perms.AddSetting("Players", SettingsFile.SubJson(defaultPermList), "List of players with modified admin-status or permission level. defafult permission level is 0. some commands can be admin-only, OR require a higher perm level, OR BOTH.");

                perms.AddSetting(defaultPermList);


                perms.ReadSettings();

                
                if(perms.TryGetSetting("players",out permList))
                {
                    Citrus.log.Log("Player Perm List loaded!");
                    foreach(PermList.PermPlayer ply in permList.players)
                    {
                    Citrus.log.Log(string.Format("{0} ({1}), PermLevel: {2}",ply.name,ply.epic,ply.permlevel));
                }
                }
                else
                {
                    Citrus.log.LogError("missing player permission list?");
                }
            
        }

        /// <summary>
        /// aww yeah.
        /// 
        /// Adds a Chat command that players, which an appropriate perm level, can execut in-game.
        /// </summary>
        /// <param name="names">an array of names that would invoke this command</param>
        /// <param name="function">An action that takes further chat parameters, and the excecuting player, as input. this is where you write what the command does!</param>
        /// <param name="adminOnly">If the player has to be an Admin to excecute this function. I reccomend using permlevel instead...</param>
        /// <param name="modName">The name of the mod this command is from. helpful for sorting, when the commands are printed to a file.</param>
        /// <param name="description">The description of the command, for when it is printed</param>
        /// <param name="paramDesc">a string example of the parameters used in the function, for printing</param>
        public static void AddCommand(string[] names, Action<string[], TABGPlayerServer> function, string modName = "", string description = "", string paramDesc = "")
        {

            foreach (string name in names)
            {
                AddCommand(name, function, modName, description, paramDesc);
                paramDesc = "";
                description = "";
            }
        }

        internal static bool RunCommand(string name, string[] prms, TABGPlayerServer player)
        {
            Command cmd = null;
            if (commands.TryGetValue(name, out cmd))
            {
                return cmd.Run(prms, player);

            }
            else
            {
                Citrus.SelfParrot(player, "unknown command: " + name);
                return false;
            }
        }

        /// <summary>
        /// ohh yeah.
        /// 
        /// Adds a Chat command that players, which an appropriate perm level, can execut in-game.
        /// </summary>
        /// <param name="name">the name of the command</param>
        /// <param name="function">An action that takes further chat parameters, and the excecuting player, as input. this is where you write what the command does!</param>
        /// <param name="adminOnly">If the player has to be an Admin to excecute this function. I reccomend using permlevel instead...</param>
        /// <param name="modName">The name of the mod this command is from. helpful for sorting, when the commands are printed to a file.</param>
        /// <param name="description">The description of the command, for when it is printed</param>
        /// <param name="paramDesc">a string example of the parameters used in the function, for printing</param>
        /// <param name="permLevel">the required perm level for the command.</param>
        public static void AddCommand(string name, Action<string[], TABGPlayerServer> function, string modName = "", string description = "", string paramDesc = "", int permLevel = 1)
        {
            if (commands.ContainsKey(name))
            {
                Citrus.log.LogError("command " + name + " already registered by another mod!");
                return;
            }
            if (function == null)
            {
                Citrus.log.LogError("Refusing to register command " + name + " as it has no function!");
                return;
            }
            commands.Add(name, new Command(name, function, modName, description, paramDesc,permLevel));


        }


        static int CommandCompare(Command a, Command b)
        {
            return a.permLevel.CompareTo(b.permLevel);
        }

        internal static void WriteAllCommands()
        {
            string f = "COMMANDS LIST:";
            Citrus.log.Log("Writing Command List to file!");
            Dictionary<string, List<Command>> sort = new Dictionary<string, List<Command>>();
            //sort.Add("General", new List<Command>());

            foreach (Command c in commands.Values)
            {
                string key = c.modName != "" ? c.modName : "Unnamed Commands";
                if (!sort.ContainsKey(key))
                {
                    sort.Add(key, new List<Command>());

                }
                sort[key].Add(c);

            }

            foreach (List<Command> lc in sort.Values)
            {
                f += "\n\n";
                f += "======" + (lc[0].modName != "" ? lc[0].modName : "General\n") + "======";

                lc.Sort(CommandCompare);

                foreach (Command c in lc)
                {
                    f += "\n\n";
                    f += c.ToString();
                }
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            string text = Path.Combine(directoryInfo.Parent.FullName, "Commands_List.txt");
            File.WriteAllText(text, f);

        }
    }


    [Serializable]
    internal class PermList : SettingObject
    {
        [JsonProperty(Order = 1)]
        public List<PermPlayer> players = new List<PermPlayer>();
        [Serializable]
        public class PermPlayer
        {
            public string name;
            public string epic;
            public int permlevel; // perm level of one sets the player to admin, higher levels allow higher permission access.

        }



    }


    class Command
    {
        string name;
        public Action<string[], TABGPlayerServer> func;
        string description;
        string paramDesc;
        public readonly string modName;
        public readonly int permLevel;



        public Command(string n, Action<string[], TABGPlayerServer> f, string mName = "", string desc = "", string pDesc = "", int plev=1)
        {
            name = n;
            func = f;
            description = desc;
            paramDesc = pDesc;
            modName = mName;
            permLevel = plev;
        }

        public override string ToString()
        {


            string ret = description;

            if (ret != "")
            {
                ret += "\n";
            }
            ret += string.Format("Perm Level: {0}\n",permLevel);

            ret += "/" + name + " " + paramDesc;
            return ret;
        }

        public bool Run(string[] prms, TABGPlayerServer player)
        {
            if (player == null)
            {
                return false;
            }
            
                
            
            int plev = 0;
            if (Citrus.permList != null)
            {
                PermList.PermPlayer ply = Citrus.permList.players.Find(p => p.epic == player.EpicUserName);
                if (ply != null)
                {
                    plev = ply.permlevel;
                }
            }
            if (plev < permLevel)
            {
                Citrus.log.Log(string.Format("Player {0} tried to use command {1} but isn't a high enough permission level", player.PlayerName, name));
                Citrus.SelfParrot(player, "Command \"" + name + "\" requires a higher permission level.");
                return false;
            }



            //runs the function!
            func(prms, player);
            return true;
        }
    }

}
