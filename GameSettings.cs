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
using Newtonsoft.Json;


namespace CitrusLib
{
    

    public static partial class Citrus
    {

        internal static SettingsFile<GameSetting> ExtraSettings = new SettingsFile<GameSetting>("ExtraSettings");



    }
    
    /// <summary>
    /// A serializable setting, used when making custom settings files.
    /// </summary>
    [Serializable]
    public class SettingObject
    {
        /// <summary>
        /// the name of the setting
        /// </summary>
        [JsonProperty(Order = -2)]
        public string name;
        /// <summary>
        /// a description of the setting
        /// </summary>
        [JsonProperty(Order = -1)]
        public string description;


    }

    /// <summary>
    /// A Settings file, containing a list of SettingObjects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SettingsFile<T> where T: SettingObject
    {
        static CitLog setLog = new CitLog("CitrusLib-Settings", ConsoleColor.Cyan);
        //public static List<SettingCategory> settings = new List<SettingCategory>();

        //public static List<SettingCategory> defaults = new List<SettingCategory>();

        internal List<T> settings = new List<T>();

        internal List<T> defaults = new List<T>();

        //static SettingFile settings;

        /// <summary>
        /// Sets a setting and then writes all settings. use this for mid-match setting changes, via commands for example.
        /// </summary>
        /// <param name="name">the name of a setting to change</param>
        /// <param name="value">the value to set it to</param>
        public bool SetSetting(string name, string value)
        {
            T g =
            settings.Find(p => p.name == name);
            if (g == null) return false;

            

            WriteSettings();
            return true;
        }

        //when setting a setting to json
        internal static string SubJson(object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented).Replace("\"","'");
        }
        

        string path;

        /// <summary>
        /// creates a settings file at the specified path.
        /// </summary>
        /// <param name="path">The filepath. if absolute is set to false, the filepath starts in the application's directory</param>
        /// <param name="absolute">if true, the filepath is relative to the application's install location</param>
        public SettingsFile(string path, bool absolute = false)
        {
            this.path = path+".txt";

            string text = path + ".txt";
            if (!absolute)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
                text = Path.Combine(directoryInfo.Parent.FullName, path + ".txt");
            }

            path = text;
        }


        /// <summary>
        /// reads the settings from it's file. if it doesn't exist, a file with default values is created instead.
        /// If the file exists, but some settings are missing, those missing settings are set to their defaults and added to the file
        /// </summary>
        public void ReadSettings()
        {
            //settings.Add("test", "test");

            settings = new List<T>();

            setLog.Log("Reading Settings from "+path);

            if (!File.Exists(path))
            {
                setLog.Log("...The settings there were missing... writing up a default file");
                File.WriteAllText(path, JsonConvert.SerializeObject(defaults,Formatting.Indented));
                
            }

            string json = File.ReadAllText(path);
            bool needsWrite = false;
            //JsonSerializerSettings js = new JsonSerializerSettings();
            

            try
            {
                settings = JsonConvert.DeserializeObject<List<T>>(json);
                setLog.Log(string.Format("settings file at {0} was read successfully", path));
            }
            catch(Exception e)
            {
                setLog.LogError(string.Format("settings file at {0} could not be read!!! resetting to defaults",path));
                File.WriteAllText(path, JsonConvert.SerializeObject(defaults, Formatting.Indented));
                settings = defaults;
                needsWrite = true;
                setLog.LogError(e.Message +'\n' +e.StackTrace);
            }

            

            

            


                foreach (T g in defaults)
                {
                    if (settings.Find(p=>p.name == g.name)==null)
                    {
                        //setting is missing from text file... need to rewrite the whole text file!
                        setLog.Log(string.Format("settings file was missing default setting {0}", g.name));
                        needsWrite = true;
                        settings.Add(g);
                    }
                }
            

            

            if (needsWrite)
            {
                WriteSettings();
            }

        }


        /// <summary>
        /// writes all the settings current values to it's file.
        /// </summary>
        public void WriteSettings()
        {

            setLog.Log("writing extra settings!");



            string write = JsonConvert.SerializeObject(settings, Formatting.Indented);





            File.WriteAllText(path, write);

            
        }


        //i forget what i was doing with this, ignore for now
        internal string GetString(object o)
        {
            if (o.GetType() == typeof(List<Vector3>))
            {
                string ret = "";
                List<Vector3> l = (List<Vector3>)o;

                if (l.Count() == 0) return "";
                ret = l.First().ToString();
                if (l.Count() == 1) return ret;

                bool first = true;
                foreach (Vector3 vec in l)
                {
                    if (first)
                    {
                        first = false;
                        continue;

                    }
                    ret += "," + vec.ToString();
                }
                return ret;
            }




            return o.ToString();
        }




        /// <summary>
        /// adds a SettingObject to the DEFAULT settings list. use when initializing the default settings, BEFORE ever reading!
        /// </summary>
        /// <param name="g">The SettingObject to add</param>
        /// <returns></returns>
        public bool AddSetting(T g)
        {
            g.name = g.name.ToLower();
            if (defaults.Find(p=>p.name == g.name)!=null)
            {
                setLog.LogError(string.Format("Tried adding setting \"{0}\" but it already exists?", g.name));
                return false;
            }

            defaults.Add(g);
            return true;
        }

        /// <summary>
        /// looks for a setting with name and returns true if found
        /// </summary>
        /// <param name="name">the name of the setting</param>
        /// <param name="g">a SettingObject out parameter</param>
        /// <returns></returns>
        public bool TryGetSetting(string name, out T g)
        {
            name = name.ToLower();
            g = null;
            
                T kvp = settings.Find(p => p.name.ToLower() == name);
                if (kvp != null)
                {
                    g = kvp;
                    return true;
                }


            



            return false;

        }





        

    }

    /// <summary>
    /// a common purpose SettingObject. Has a string value. it's reccomended to use this when making settings
    /// </summary>
    [Serializable]
    public class GameSetting : SettingObject
    {
        /// <summary>
        /// the value of the setting
        /// </summary>
        [JsonProperty(Order = 1)]
        public string value; //current value
        
        /*
        public GameSetting(string n, object v, string d = "")
        {
            name = n.ToLower();
            value = v.ToString();
            description = d;
        }
        */


    }


}
