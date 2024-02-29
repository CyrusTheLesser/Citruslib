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
        public static PermList permList;

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
                    defaultPermList.description = "List of players with modified admin-status or permission level. defafult permission level is 0. some commands can be admin-only, OR require a higher perm level, OR BOTH.";

                PermList.PermPlayer dp = new PermList.PermPlayer();
                dp.admin = true;
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
                    Citrus.log.Log(string.Format("{0} ({1}), Admin: {2}, PermLevel: {3}",ply.name,ply.epic,ply.admin,ply.permlevel));
                }
                }
                else
                {
                    Citrus.log.LogError("missing player permission list?");
                }
            
        }

        public static void AddCommand(string[] names, Action<string[], TABGPlayerServer> function, bool adminOnly = true, string modName = "", string description = "", string paramDesc = "")
        {

            foreach (string name in names)
            {
                AddCommand(name, function, adminOnly, modName, description, paramDesc);
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

        public static void AddCommand(string name, Action<string[], TABGPlayerServer> function, bool adminOnly = true, string modName = "", string description = "", string paramDesc = "", int permLevel = 1)
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
            commands.Add(name, new Command(name, function, adminOnly, modName, description, paramDesc,permLevel));


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
    public class PermList : SettingObject
    {
        [JsonProperty(Order = 1)]
        public List<PermPlayer> players = new List<PermPlayer>();
        [Serializable]
        public class PermPlayer
        {
            public string name;
            public string epic;
            public bool admin;
            public int permlevel; // perm level of one sets the player to admin, higher levels allow higher permission access.

        }



    }


    class Command
    {
        string name;
        public Action<string[], TABGPlayerServer> func;
        bool adminonly;
        string description;
        string paramDesc;
        public readonly string modName;
        public readonly int permLevel;



        public Command(string n, Action<string[], TABGPlayerServer> f, bool a, string mName = "", string desc = "", string pDesc = "", int plev=1)
        {
            name = n;
            func = f;
            adminonly = a;
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
            ret += string.Format("Admin: {0}, Perm Level: {1}\n",adminonly.ToString(),permLevel);

            ret += "/" + name + " " + paramDesc;
            return ret;
        }

        public bool Run(string[] prms, TABGPlayerServer player)
        {
            if (player == null)
            {
                return false;
            }
            if (adminonly & !player.IsAdmin)
            {
                bool admin = false;
                if (Citrus.permList != null)
                {
                    PermList.PermPlayer ply = Citrus.permList.players.Find(p => p.epic == (string)player.EpicUserName);
                    if (ply != null)
                    {
                        Citrus.log.Log(string.Format("Setting player {0} to admin as they weren't already", player.PlayerName));
                        player.SetAdmin(new string[] { player.SteamID });
                        admin = true;
                    }
                }


                if (!admin)
                {
                    Citrus.log.Log(string.Format("Player {0} tried to use command {1} but isnt admin!", player.PlayerName, name));
                    Citrus.SelfParrot(player, "Command \"" + name + "\" is admin only");
                    return false;
                }
                
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
