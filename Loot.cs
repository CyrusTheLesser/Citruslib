using Landfall.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrusLib
{
    /// <summary>
    /// A class for creating groups of items, to simplify item-giving
    /// </summary>
    public class LootPack
    {
        List<int> types;
        List<byte> amounts;

        /// <summary>
        /// makes a new, empty lootpack.
        /// </summary>
        public LootPack()
        {
            types = new List<int>();
            amounts = new List<byte>();
            
        }

        /// <summary>
        /// creates a lootpack using TABG's Loot array as an input
        /// </summary>
        /// <param name="loot">An array of Loot structs.</param>
        public LootPack(Loot[] loot)
        {
            types = new List<int>();
            amounts = new List<byte>();

            foreach(Loot l in loot)
            {
                types.Add(l.loot.GetComponent<Pickup>().m_itemIndex);
                amounts.Add((byte)l.quanitity);
            }
        }

        /// <summary>
        /// adds a stack of an item to the loot pack.
        /// </summary>
        /// <param name="id">the id of the item. use the Citlib Item enum!</param>
        /// <param name="amount">the amount to add.</param>
        public void AddLoot(int id, int amount)
        {
            if (amount > (int)byte.MaxValue)
            {
                AddLoot(id, amount - 254);
                amount = 254;
            }
            types.Add(id);
            amounts.Add((byte)amount);
        }


        /// <summary>
        /// creates a lootpack that is a copy of the provided list of playerlootitems, which can be read from a TABGPlayerServer.
        /// </summary>
        /// <param name="loot"></param>
        /// <returns></returns>
        public static LootPack CopyPlayerLoot(List<TABGPlayerLootItem> loot)
        {
            LootPack ret = new LootPack();

            foreach(TABGPlayerLootItem item in loot)
            {
                ret.AddLoot(item.ItemIdentifier, item.ItemCount);
            }


            return ret;
        }

        /// <summary>
        /// gives the items to the player. the player should be alive
        /// </summary>
        /// <param name="player">the player to recieve the items</param>
        public void GiveTo(TABGPlayerServer player)
        {
            byte[] buffer = new byte[2 + types.Count() * 9];

            //List<int> ids = new List<int>();



            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {

                    binaryWriter.Write(player.PlayerIndex);
                    binaryWriter.Write((byte)types.Count());
                    for (int k = 0; k < types.Count(); k++)
                    {
                        int id = Citrus.World.GameRoomReference.GetNewWeaponIndex();
                        NetworkGun networkGun = new NetworkGun("DEV GUN", (int)amounts[k], id, types[k], null, false);
                        player.AddLoot(networkGun);
                        //does nothing
                        //Citrus.World.GameRoomReference.CurrentGameMode.HandlePlayerPickup(player, networkGun);

                        binaryWriter.Write(id);
                        binaryWriter.Write(types[k]);
                        binaryWriter.Write(amounts[k]);
                    }
                }
            }

            //world peace
            Citrus.World.SendMessageToClients(EventCode.LootPackGiven, buffer, player.PlayerIndex, true);



        }

    }

    /// <summary>
    /// an enum list of every vanilla item. helps hard-coding item-give commands and lootpacks easier
    /// </summary>

    public enum Item : int
    {
#pragma warning disable CS1591 //im not writing docs on every item
        _45_ACP = 0,
        _Big_Ammo = 1,
        _Bolts = 2,
        _Money_Ammo = 3,
        _Musket_Ammo = 4,
        _No_ammo = 5,
        _Normal_Ammo = 6,
        _Rocket_Ammo = 7,
        _Shotgun_Ammo = 8,
        _Small_Ammo = 9,
        _Soul = 10,
        _Taser_Ammo = 11,
        _Water_Ammo = 12,
        _Lv0_JetpackArmor = 13,
        _Lv1_Safety_Vest = 14,
        _Lv2_Kevlar_Vest = 15,
        _Lv3_Big_Boy_Kevlar_Vest = 16,
        _Lv4_Heavy_Armor = 17,
        _Lv5_Banana_Armor = 18,
        _Lv5_Pickle_Armor = 19,
        _05xScope = 20,
        _2xScope = 21,
        _4xScope = 22,
        _8xScope = 23,
        _Compensator = 24,
        _Damage_Analyzer = 25,
        _Healing_Barrel = 26,
        _Health__Analyzer = 27,
        _Heavy_Barrel = 28,
        _Laser_Sight = 29,
        _Double_Barrel = 30,
        _Fast_Barrel = 31,
        _Accuracy_Barrel = 32,
        _Fire_Rate_Barrel = 33,
        _Big_Slow_Bullet_Barrel = 34,
        _Periscope_Barrel = 35,
        _Periscope = 36,
        _Recycler = 37,
        _Red_Dot = 38,
        _Suppressor = 39,
        _Suppressor002 = 40,
        _Tazer_Barrel = 41,
        _Common_bloodlust = 42,
        _Common_cardio = 43,
        _Common_dash = 44,
        _Common_health = 45,
        _Common_ice = 46,
        _Common_jump = 47,
        _Common_poison = 48,
        _Common_recycling = 49,
        _Common_regeneration = 50,
        _Common_relax = 51,
        _Common_shield = 52,
        _Common_speed = 53,
        _Common_spray = 54,
        _Common_storm = 55,
        _Common_the_hunt = 56,
        _Common_vampire = 57,
        _Common_weapon_mastery = 58,
        _Epic_battlecry = 59,
        _Epic_bloodlust = 60,
        _Epic_cardio = 61,
        _Epic_charge = 62,
        _Epic_dash = 63,
        _Epic_healing_words = 64,
        _Epic_health = 65,
        _Epic_ice = 66,
        _Epic_jump = 67,
        _Epic_lit_beats = 68,
        _Epic_poison = 69,
        _Epic_recycling = 70,
        _Epic_regeneration = 71,
        _Epic_relax = 72,
        _Epic_shield = 73,
        _Epic_small = 74,
        _Epic_speed = 75,
        _Epic_spray = 76,
        _Epic_storm_call = 77,
        _Epic_storm = 78,
        _Epic_the_hunt = 79,
        _Epic_vampire = 80,
        _Epic_weapon_mastery = 81,
        _Epic_words_of_justice = 82,
        _Legendary_battlecry = 83,
        _Legendary_bloodlust = 84,
        _Legendary_cardio = 85,
        _Legendary_charge = 86,
        _Legendary_dash = 87,
        _Legendary_healing_words = 88,
        _Legendary_health = 89,
        _Legendary_ice = 90,
        _Legendary_jump = 91,
        _Legendary_lit_beats = 92,
        _Legendary_poison = 93,
        _Legendary_recycling = 94,
        _Legendary_regeneration = 95,
        _Legendary_relax = 96,
        _Legendary_shield = 97,
        _Legendary_speed = 98,
        _Legendary_spray = 99,
        _Legendary_storm_call = 100,
        _Legendary_storm = 101,
        _Legendary_the_hunt = 102,
        _Legendary_vampire = 103,
        _Legendary_weapon_mastery = 104,
        _Legendary_words_of_justice = 105,
        _Rare_airstrike = 106,
        _Rare_bloodlust = 107,
        _Rare_cardio = 108,
        _Rare_dash = 109,
        _Rare_health = 110,
        _Rare_ice = 111,
        _Rare_insight = 112,
        _Rare_jump = 113,
        _Rare_lit_beats = 114,
        _Rare_poison = 115,
        _Rare_pull = 116,
        _Rare_recycling = 117,
        _Rare_regeneration = 118,
        _Rare_relax = 119,
        _Rare_shield = 120,
        _Rare_speed = 121,
        _Rare_spray = 122,
        _Rare_storm = 123,
        _Rare_the_hunt = 124,
        _Rare_vampire = 125,
        _Rare_weapon_mastery = 126,
        _The_assassin = 127,
        _The_mad_mechanic = 128,
        _Blessing_pickup = 129,
        _Transcention_Orb = 130,
        _Bandage = 131,
        _Med_Kit = 132,
        _Lv1_Bike_Helmet = 133,
        _Lv2_Fast_Motorcycle_Helmet = 134,
        _Lv2_Fastest_Motorcycle_Helmet = 135,
        _Lv2_Motorcycle_Helmet_Open = 136,
        _Lv2_Motorcycle_Helmet = 137,
        _Lv2_Old_School_Motorcycle_Helmet = 138,
        _Lv3_Grey_Kevlar_Helmet_with_Googles = 139,
        _Lv3_Grey_Kevlar_Helmet = 140,
        _Lv3_Kevlar_Helmet_With_Googles = 141,
        _Lv3_Kevlar_Helmet = 142,
        _Lv4_Heavy_Helmet_Open_1 = 143,
        _Lv4_Cowboy_Hat = 144,
        _Lv4_Explosiveguy_Hat = 145,
        _Lv4_Heavy_Helmet_Open = 146,
        _Lv4_Heavy_Helmet = 147,
        _Lv4_Knight_Helmet = 148,
        _Lv4_Rambo_Bandana = 149,
        _Lv4_Tricorne_Hat = 150,
        _AK2K47 = 151,
        _AK47=152,
        _AUG = 153,
        _BeamAR = 154,
        _Burstgun = 155,
        _Famas = 156,
        _Cursed_Famas = 157,
        _H1 = 158,
        _Liberating_M16 = 159,
        _M16 = 160,
        _MP44=161,
        _SCARH = 162,
        _Automatic_Crossbow = 163,
        _Balloon_Crossbow = 164,
        _AK47_2=165,
        _AK47_3=166,
        _AK47_4=167,
        _BOMB = 168,
        _Crossbow = 169,
        _Taser_Crossbow = 170,
        _Fireowork_Crossbow = 171,
        _Gaussbow = 172,
        _Grappling_hook = 173,
        _Harpoon = 174,
        _Mini_Gun = 175,
        _Liberating_Mini_Gun = 176,
        _Mega_Gun = 177,
        _Mini_Gun_2 = 178,
        _MissileLauncher = 179,
        _Money_Stack = 180,
        _Smoke_Rocket_Launcher = 181,
        _Rocket_Launcher = 182,
        _Taser_Mini_Gun = 183,
        _The_Promise = 184,
        _Water_Gun = 185,
        _Grenade = 186,
        _BIG_Healing_Grenade = 187,
        _Black_Hole_Grenade = 188,
        _Bombardement_Grenade = 189,
        _Bouncy_Grenade = 190,
        _Cage_Grenade = 191,
        _Taser_Cage_Grenade = 192,
        _Cluster_Grenade = 193,
        _Cluster_Dummy_Grenade = 194,
        _Dummy_Grenade = 195,
        _Fire_Grenade = 196,
        _Grenade_2 = 197,
        _Healing_Grenade = 198,
        _Implosion_Grenade = 199,
        _Knockback_Grenade = 200,
        _BIG_Knockback_Grenade = 201,
        _Launch_Pad_Grenade = 202,
        _MGL = 203,
        _Orbital_Tase_Grenade = 204,
        _Orbital_Strike_Grenade = 205,
        _Poof_Grenade = 206,
        _Shield_Grenade = 207,
        _Smoke_Grenade = 208,
        _Snow_Storm_Grenade = 209,
        _Splinter_Grenade = 210,
        _Taser_Splinter_Grenade = 211,
        _Stun_Grenade = 212,
        _Snow_Storm_Grenade_2 = 213,
        _Dynamite = 214,
        _Volley_Grenade = 215,
        _Wall_Grenade = 216,
        _Browning_M2 = 217,
        _M1918BAR = 218,
        _M8 = 219,
        _MG42=220,
        _Spell_Blinding_light = 221,
        _Spell_Gravity_Field = 222,
        _Spell_Gust = 223,
        _Spell_Healing_aura = 224,
        _Spell_Speed_aura = 225,
        _Spell_Summon_rock = 226,
        _Spell_Teleport = 227,
        _Spell_Track = 228,
        _Spell_Fire_ball = 229,
        _Spell_Ice_bolt = 230,
        _Spell_Magic_missile = 231,
        _Spell_Mirage = 232,
        _Spell_Orb_of_sight = 233,
        _Spell_Reveal = 234,
        _Spell_Shockwave = 235,
        _Spell_Summon_tree = 236,
        _Spell_Track2 = 237,
        _Ballistic_Shield = 238,
        _Ballistic_Shields = 239,
        _Taser_Ballistic_Shield = 240,
        _Baton = 241,
        _Black_Katana = 242,
        _Boxing_Glove = 243,
        _Cleaver = 244,
        _Crowbar = 245,
        _Crusader_Sword = 246,
        _Taser_Crusader_Sword = 247,
        _Fish = 248,
        _Taser_Fish = 249,
        _Holy_Sword = 250,
        _Inflatable_Hammer = 251,
        _Jarl_Axe = 252,
        _Taser_Jarl_Axe = 253,
        _Katana = 254,
        _Knife = 255,
        _Rapier = 256,
        _Riot_Shield = 257,
        _Sabre = 258,
        _Shallow_Pot_With_Long_Handle = 259,
        _Shield = 260,
        _Shovel = 261,
        _Viking_Axe = 262,
        _Weights = 263,
        _Beretta_93R = 264,
        _Crossbow_pistol = 265,
        _Desert_Eagle = 266,
        _Flintlock = 267,
        _Taser_Flintlock = 268,
        _Auto_Revolver = 269,
        _Wind_up_pistol = 270,
        _G18c = 271,
        _Glue_gun = 272,
        _Hand_Gun = 273,
        _HandCannon = 274,
        _Liberating_m1911 = 275,
        _Luger_P08 = 276,
        _m1911 = 277,
        _Real_Gun = 278,
        _Really_Big_Deagle = 279,
        _Revolver = 280,
        _Holy_Revolver = 281,
        _Revolver_2 = 282,
        _Hardballer = 283,
        _Taser = 284,
        _Beam_DMR = 285,
        _FAL = 286,
        _Garand = 287,
        _Liberating_Garand = 288,
        _M14 = 289,
        _S7 = 290,
        _Winchester_Model1886 = 291,
        _AA_12=292,
        _Blunderbuss = 293,
        _Sawed_off_Shotgun = 294,
        _Flying_Blunderbuss = 295,
        _Liberating_AA_12=296,
        _Mossberg_500=297,
        _Mossberg_5000=298,
        _Taser_Mossberg_500=299,
        _Rainmaker = 300,
        _The_Arnold = 301,
        _AKS_74U=302,
        _AWPS_74U=303,
        _Money_maker_mac = 304,
        _Glockinator = 305,
        _Liberating_M1a1_Thompson = 306,
        _M1a1_Thompson = 307,
        _Mac_10=308,
        _MP_40=309,
        _MP5_K = 310,
        _P90 = 311,
        _PPSH_41=312,
        _Tec_9=313,
        _UMP_45=314,
        _Vector = 315,
        _Z4 = 316,
        _AWP = 317,
        _Taser_AWP = 318,
        _Barrett = 319,
        _Beam_Sniper = 320,
        _Kar98K = 321,
        _Liberating_Barrett = 322,
        _Musket = 323,
        _Taser_Musket = 324,
        _Really_Big_Barrett = 325,
        _Sniper_Shotgun=326,
        _Double_Shot = 327,
        _VSS = 328
#pragma warning restore CS1591
    }

}
