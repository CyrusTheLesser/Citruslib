using Epic.OnlineServices;
using Landfall.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CitrusLib
{
    //logs every unique player who connects to the server, useful for moderation. 
    static class GuestBook
    {

        static Dictionary<Utf8String, string> guestBook = new Dictionary<Utf8String, string>();




        internal static void SignGuestBook(TABGPlayerServer player)
        {
            string data = string.Format("{0}, Playfab={1},Steam={2}",player.PlayerName,player.PlayFabID,player.SteamID);
            if (guestBook.ContainsKey(player.EpicUserName))
            {
                guestBook[player.EpicUserName] = data;
            }
            else
            {
                guestBook.Add(player.EpicUserName, data);
            }

            List<string> lines = new List<string>();

            foreach(KeyValuePair<Utf8String,string> kvp in guestBook)
            {
                lines.Add(kvp.Key+":"+kvp.Value);
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            string text = Path.Combine(directoryInfo.Parent.FullName, "Guestbook.txt");
            File.WriteAllLines(text, lines.ToArray());
        }

        internal static void LoadGuestBook()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            string text = Path.Combine(directoryInfo.Parent.FullName, "Guestbook.txt");
            guestBook = new Dictionary<Utf8String, string>(); 
            if (!File.Exists(text))
            {
                File.WriteAllText(text, "");
            }
            string[] lines = File.ReadAllLines(text);
            foreach(string s in lines)
            {
                if (s.Contains(":"))
                {
                    guestBook.Add(s.Split(':')[0], s.Split(':')[1]);
                }
            }
        }


    }
}
