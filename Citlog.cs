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


    //logging changes and stuff
    public static partial class Citrus
    {
        //my logger for Citruslib debugging. dont use it! make your own CitLog!
        public static CitLog log = new CitLog("CitrusLib", ConsoleColor.Cyan);

        public static bool landLogSupressed = false;

        //takes over landfall's logger to have it's format match mine.
        public static CitLog landLog = new CitLog("LandLog", ConsoleColor.Gray);

        



    }

    public class CitLog
    {
        string modName;
        ConsoleColor modColor;

        static string lastMessage = "";
        static int combo = 1;

        /// <summary>
        /// Makes a new CitLog for logging stuff in your mod!
        /// </summary>
        /// <param name="n">The name of your mod, or a sub-name if your mod has multiple seperate parts</param>
        /// <param name="c">The color of the name.</param>
        public CitLog(string n, ConsoleColor c = ConsoleColor.White)
        {
            modName = n;
            modColor = c;
        }

        public void Log(string text, bool error = false)
        {
            if (this == Citrus.landLog & Citrus.landLogSupressed)
            {
                return;
            }

            if (Compare(text, lastMessage) / Mathf.Max(1f, Mathf.Max(text.Length, lastMessage.Length)) < 0.15f)
            {
                ClearLastLine();
                combo++;
            }
            else
            {
                combo = 1;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");
            Console.ForegroundColor = modColor;
            Console.Write(modName);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] - ");
            Console.ForegroundColor = error ? ConsoleColor.Red : ConsoleColor.Gray;
            {
                Console.Write(text);
            }
            if (combo > 1)
            {

                Console.ForegroundColor = ConsoleColor.Magenta;
                if (combo < 10)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else if (combo < 25)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (combo < 50)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                }
                else if (combo < 100)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (combo < 500)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                }
                else
                {
                    Console.ForegroundColor = combo % 2 == 0 ? ConsoleColor.DarkRed : ConsoleColor.Yellow;
                    //holy moly
                }


                Console.Write(string.Format(" (COMBO: {0})", combo));


            }

            if (error)
            {
                Debug.LogError("");
            }
            else
            {
                Console.WriteLine("");
            }


            lastMessage = text;
        }

        public void LogError(string text)
        {
            Log(text, true);
        }

        static void ClearLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        static int Compare(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }


    }

}