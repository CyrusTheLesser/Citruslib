using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CitrusLib
{
    class CustomLootTables
    {

        internal static string LOOTTABLEKEY = "loot table location";

        static string FilePath = null;

        internal static MatchModifier[] vanillaMods;

        public static List<LootTable> LootTables;

        static string path
        {
            get
            {
                if (FilePath != null)
                {
                    return FilePath;
                }
                else
                {
                    GameSetting g;

                    if (!Citrus.ExtraSettings.TryGetSetting(LOOTTABLEKEY, out g))
                    {
                        Citrus.log.LogError("filepath setting for loottables is missing!");
                    }

                    string res = g.value;


                    
                    bool absolute = res != "";
       


                    //path = path + ".txt";

                    string text = res;
                    if (!absolute)
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
                        text = directoryInfo.Parent.FullName;
                        
                    }
                    text = Path.Combine(text, "LootTables");
                    FilePath = text;

                    return FilePath;
                    
                }
            }
        }

        //plan: designate folder location in ExtraSettings. EVERY file inside MUST be a valid json loot table, all of which will override the static loot table that loads on... load.

        //honestly, this doesnt need to be in a seperate file but it may as well be for better feature organization.

        //...in fact, i should probably break main up into smaller files. all those chat commands...

        //register settings
        internal static void Register()
        {
            Citrus.ExtraSettings.AddSetting(new GameSetting()
            {
                name = LOOTTABLEKEY,
                description = "The FOLDER that will hold a loottables FOLDER inside of it, which, in turn, will hold the customizable loot tables. LEAVE BLANK for a local folder",
                value = ""
            });
        }




        //loads all the loottables from the folder. if the folder is empty or doesnt exist, it populates the folder with "example" tables: exact copies of the vanilla loot tables!
        public static void ReadLoot()
        {

            Citrus.log.Log("Reading Loot Table Files at "+ path);
            if (!Directory.Exists(path))
            {
                Citrus.log.Log("creating loot folder");
                Directory.CreateDirectory(path);
            }

            List<string> names = Directory.GetFiles(path,"*.json").ToList();

            //filter out any non-json files (BUT THERE SHOULD BE NONE!)

            //List<string> files = names.Where(n => n.EndsWith(".json")).ToList();
            
            if (names.Count == 0)
            {
                //darn...
                Citrus.log.Log("no json files in the filepath folder! adding defaults");
                CreateDefaultLoot();
                
            }
                
            LootTables = new List<LootTable>();

            foreach (string data in names.ConvertAll((s)=>
            {
                Citrus.log.Log( Path.GetFileName(s));
                return File.ReadAllText(s); //so cool
            }))
            {
                LootTables.Add(JsonConvert.DeserializeObject<LootTable>(data));
            }



        }

        //rolls a random Loot Table (identical to tabg's MatchModifier) using customizeable weights
        public static MatchModifier RandomMatchModifier()
        {
            if(LootTables == null)
            {
                Citrus.log.Log("huh. randommatchmod is called before gamestart? loading loottables early then!");
                ReadLoot();
            }

            float total = 0;

            if (LootTables.Count() == 0)
            {
                Citrus.log.LogError("no loot table, still???");
                return null;
            }

            foreach(LootTable lt in LootTables)
            {
                total += lt.weight;
            }

            //maaan i forgot i need to make a matchmodifier somehow

            float num= UnityEngine.Random.Range(0f, total);

            foreach (LootTable lt in LootTables)
            {
                num -= lt.weight;

                if (num <= 0)
                {
                    return ToModifier(lt);
                }
            }


            return null; //:)
        }


        static MatchModifier ToModifier(LootTable lt)
        {

            //guh
            MatchModifier mm = new MatchModifier
            {
                ModifierTitle = lt.name,
                Index = lt.index
                
            };
            //OH AND THE LOOT PRESET TOO

            LootPreset lp =  (LootPreset)ScriptableObject.CreateInstance(typeof(LootPreset));

            lp.m_forceSpawn = false;

            lp.loot = new List<LootDropWrapper>();
            //oh, and the loot wrappers...

            foreach(LootTableEntry le in lt.entries)
            {
                List<Loot> li = new List<Loot>();
                foreach(LootTableItem lti in le.entries) //the naming conventions arent confusing yet
                {
                    GameObject lGo = PickupObject(lti.id);

                    Loot loot = new Loot()
                    {
                        quanitity = lti.amount,
                        loot = lGo
                        
                    };

                    li.Add(loot);
                }

                LootDropWrapper ldw = new LootDropWrapper(null) //probably works
                {
                    lootName = le.name,
                    spawnRate = le.weight,
                    m_loot = li.ToArray()
                };

                lp.loot.Add(ldw);
               
            }

            mm.Preset = lp;
            return mm; //hooray!
        }



        //converts the vanilla rarity to improved float rarity
        static float vRarityCalc(Curse.Rarity rarity)
        {
            float num3 = 0.1f;
            float num4 = 10f;
            float num5 = 0.5f;
            float num6 = 0.1f;
            float num7 = 0.01f;
            num5 *= num3;
            num6 *= num3;
            num7 *= num3;
            //dont ask.

            if (rarity == Curse.Rarity.Common)
            {
                return num4;
            }
            if (rarity == Curse.Rarity.Rare)
            {
                return num5;
            }
            if (rarity == Curse.Rarity.Epic)
            {
                return num6;
            }
            if (rarity == Curse.Rarity.Legendary)
            {
                return num7;
            }
            return 1;
        }


        static int PickupId(GameObject g)
        {
            Pickup p = g.GetComponent<Pickup>();

            if (p == null) return -1; //hell on earth

            return p.m_itemIndex; 
        }

        static GameObject PickupObject(int id)
        {
            return LootDatabase.Instance.GetDataEntry(id).prefab;

            /*
            Pickup ent = LootDatabase.Instance.GetAllLoot().Find((p) => p.m_itemIndex == id);

            if(ent == null)
            {
                Citrus.log.LogError(string.Format("invalid pickup id {0} when generating loot table!",id));
            }

            return ent.gameObject;*/ //i think this is okay. i hope this is okay.
        }


        static void CreateDefaultLoot()
        {
            List<LootTable> loots = new List<LootTable>();

           // Citrus.log.Log("1");
            //im not sure if this list ALSO needs to be altered/ovverwritten
            TABGLootPresetDatabase.Instance.GetAllLootPresets();

            foreach(MatchModifier m in vanillaMods)
            {
                LootTable nloot = new LootTable
                {
                    name = m.ModifierTitle,
                    weight = vRarityCalc(m.Rarity),
                    index = m.Index
                };

                nloot.entries = new List<LootTableEntry>();

                foreach (LootDropWrapper l in m.Preset.loot)
                {
                    LootTableEntry ent = new LootTableEntry
                    {
                        name = l.lootName,
                        weight = l.spawnRate
                    };
                    ent.entries = new List<LootTableItem>();

                    foreach (Loot lo in l.m_loot)
                    {
                        LootTableItem lti = new LootTableItem
                        {
                            id = PickupId(lo.loot),
                            amount = lo.quanitity
                        };
                        ent.entries.Add(lti); //adds items to entries
                    }



                    nloot.entries.Add(ent); //adds entries to tables
                }


                loots.Add(nloot);//adds loot tables to loot tables list! whew!
            }

            LootTables = loots;

            WriteAllLootTables();

        }


        static void WriteAllLootTables()
        {
            Citrus.log.Log("Writing all loot tables!");

            foreach (LootTable t in LootTables)
            {
                File.WriteAllText(path+"/"+t.name+".json", JsonConvert.SerializeObject(t, Formatting.Indented));
            }

            Citrus.log.Log("hooray!");
        }

        //secretly, its a mixup of the Matchmodifer and 
        [System.Serializable]
        public class LootTable
        {
            [JsonProperty(Order = 0)]
            public string name;
            [JsonProperty(Order = 1)]
            public float weight;
            [JsonProperty(Order = 2)]
            public int index;
            [JsonProperty(Order = 3)]
            public List<LootTableEntry> entries;
        }

        [System.Serializable]
        public class LootTableEntry
        {
            [JsonProperty(Order = 0)]
            public string name;
            [JsonProperty(Order = 1)]
            public float weight;
            [JsonProperty(Order = 2)]
            public List<LootTableItem> entries;
        }

        [System.Serializable]
        public class LootTableItem
        {
            [JsonProperty(Order = 0)]
            public int id;
            [JsonProperty(Order = 1)]
            public int amount;
        }

    }
}
