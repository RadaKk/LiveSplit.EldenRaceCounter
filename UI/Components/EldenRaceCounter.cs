using Microsoft.VisualBasic.FileIO;
using SoulMemory;
using SoulMemory.EldenRing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LiveSplit.UI.Components
{
    public class EldenRaceCounter : ICounter
    {
        private int initialValue = 0;
        private Dictionary<string, int> InitialIncrementMap = new Dictionary<string, int>();
        public Dictionary<string, int> IncrementMap { get; private set; } = new Dictionary<string, int>();
        private Dictionary<string, uint> eventToMemory = new Dictionary<string, uint>();
        private Dictionary<string, string> randomizerMapping = new Dictionary<string, string>();

        // Only consider Bosses for now
        // private List<Type> EventTypes = new List<Type> { typeof(Boss), typeof(Grace), typeof(ItemPickup) };
        private static readonly List<Type> EventTypes = new List<Type> { typeof(Boss) };

        // Default Bosses conf
        private static readonly HashSet<string> majorBosses = new HashSet<string>()
        {
            "GodrickTheGraftedStormveilCastle",
            "MargitTheFellOmenStormveilCastle",
            "MorgottTheOmenKingLeyndell",
            "GodfreyFirstEldenLordLeyndell",
            "HoarahLouxLeyndell",
            "SirGideonOfnirTheAllKnowingLeyndell",
            "DragonkinSoldierOfNokstellaAinselRiver",
            "ValiantGargoylesSiofraRiver",
            "MimicTearSiofraRiver",
            "FiasChampionDeeprootDepths",
            "LichdragonFortissaxDeeprootDepths",
            "AstelNaturalbornOfTheVoidLakeOfRot",
            "MohgLordOfBloodMohgwynPalace",
            "AncestorSpiritSiofraRiver",
            "RegalAncestorSpiritNokronEternalCity",
            "MalikethTheBlackBladeCrumblingFarumAzula",
            "DragonlordPlacidusaxCrumblingFarumAzula",
            "GodskinDuoCrumblingFarumAzula",
            "RennalaQueenOfTheFullMoonAcademyOfRayaLucaria",
            "RedWolfOfRadagonAcademyOfRayaLucaria",
            "MaleniaBladeOfMiquellaMiquellasHaligtree",
            "LorettaKnightOfTheHaligtreeMiquellasHaligtree",
            "RykardLordOfBlasphemyVolcanoManor",
            "GodskinNobleVolcanoManor",
            "EldenBeastEldenThrone",
            "MagmaWyrmMakarRuinStrewnPrecipiceLiurnia",
            "LeonineMisbegottenCastleMorneWeepingPenisula",
            "RoyalKnightLorettaCarianManorLiurnia",
            "StarscourgeRadahnBattlefieldCaelid",
            "FireGiantGiantsForgeMountaintops",
            "CommanderNiallCastleSoulMountaintops",
            "NightsCavalryAltusHighwayAltusPlateau"
        };
        private static readonly int defaultMajorBossesPoints = 10;
        private static readonly int defaultEventPoints = 2;
        private Dictionary<string, int> DefaultPointConf = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value for the counter.</param>
        public EldenRaceCounter(int initialValue = 0)
        {
            this.initialValue = initialValue;
            Count = initialValue;
            SetEventToMemory();
            setDefaultConf();
        }

        public int Count { get; private set; }

        public string lastPointsEarnedMsg { get; private set; } = string.Empty;


        private void SetLastEarnedPointMessage(string _event, int point, int maxCharPerWord = 5, int maxLength = 20)
        {
            string[] eventParts = Regex.Split(_event, @"(?<!^)(?=[A-Z](?![A-Z]|$))");
            int totalChars = 0;
            for (int i = 0; i < eventParts.Length; i++)
            {
                if (totalChars < maxLength)
                {
                    if (eventParts[i].Length > maxCharPerWord)
                    {
                        eventParts[i] = eventParts[i].Substring(0,maxCharPerWord) + ".";
                    }
                }
                totalChars += eventParts[i].Length;
            }
            lastPointsEarnedMsg = string.Format("{0}pts - {1}", point, string.Join(" ", eventParts));
        }


        private void setDefaultConf()
        {
            DefaultPointConf.Clear();
            foreach (KeyValuePair<string, uint> entry in eventToMemory)
            {
                DefaultPointConf.Add(entry.Key, majorBosses.Contains(entry.Key) ? defaultMajorBossesPoints : defaultEventPoints);

            }
        }

        /// <summary>
        /// Increments this instance.
        /// <param name="ERGame">ER instance game to search event</param>
        /// </summary>
        public bool Increment(IGame ERGame)
        {
            if (Count == int.MaxValue)
                return false;

            List<string> popKeys = new List<string>();
            int lastValue = 0;
            string lastKey = null;
            try
            {
                foreach (KeyValuePair<string, int> entry in IncrementMap)
                {
                    if (ERGame.ReadEventFlag(eventToMemory[entry.Key]))
                    {
                        Count = checked(Count + entry.Value);
                        popKeys.Add(entry.Key);
                        lastValue = entry.Value;
                        lastKey = entry.Key;
                    }
                }
            }
            catch (OverflowException)
            {
                Count = int.MaxValue;
                return false;
            }

            foreach (string key in popKeys)
            {
                IncrementMap.Remove(key);
            }

            if (lastKey != null)
            {
                SetLastEarnedPointMessage(lastKey, lastValue);
            }

            return true;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            Count = initialValue;
            IncrementMap = new Dictionary<string, int>(InitialIncrementMap);
            randomizerMapping.Clear();
            lastPointsEarnedMsg = string.Empty;
            SetEventToMemory();
            setDefaultConf();
        }

        /// <summary>
        /// Sets the count.
        /// </summary>
        public void SetCount(int value)
        {
            Count = value;
        }

        public void SetEventToMemory()
        {
            eventToMemory = new Dictionary<string, uint>();

            foreach (var type in EventTypes)
            {
                foreach (string eventName in Enum.GetNames(type))
                {
                    eventToMemory.Add(eventName, (uint)Enum.Parse(type, eventName));
                }
            }
        }

        /// <summary>
        /// Sets the map value IncrementMap from eventName to points.
        /// </summary>
        /// <param name="csvPath"></param>
        public void SetIncrement(string csvPath)
        {
            IncrementMap = new Dictionary<string, int>();
            InitialIncrementMap = new Dictionary<string, int>();

            using (TextFieldParser parser = new TextFieldParser(csvPath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                int i = 0;
                while (!parser.EndOfData)
                {
                    i++;
                    //Process row name, points
                    string[] fields = parser.ReadFields();

                    if (fields.Length != 2)
                    {
                        throw new FileFormatException("Cannot read file " + csvPath + ",  error at line " + i);
                    }

                    string eventName = fields[0];
                    int eventPoints = int.Parse(fields[1]);


                    if (!eventToMemory.ContainsKey(eventName))
                    {
                        throw new FileFormatException("Unknown event: " + eventName + " at line " + i);
                    }

                    IncrementMap[eventName] = eventPoints;
                    InitialIncrementMap[eventName] = eventPoints;

                }
            }

            applyRandomizedMapping();
        }

        public void SetDefaultIncrement()
        {
            InitialIncrementMap = new Dictionary<string, int>(DefaultPointConf);
            IncrementMap = new Dictionary<string, int>(DefaultPointConf);
        }

        public void SetRandomizerMapping(string txtFile)
        {
            // poorly optimized but it should not be a concern
            Dictionary<uint, uint> eventToRdEvent = new Dictionary<uint, uint>();
            bool isFileValid = false;

            // parse file and produce a mapping memory to memory
            using (StreamReader reader = new StreamReader(txtFile))
            {
                string line;
                int i = 0;
                bool bossSectionReached = false;

                while ((line = reader.ReadLine()) != null)
                {
                    i++;
                    //Process row name, points
                    string[] fields = line.Split(' ');

                    if (fields.Length >= 2 && fields[0].StartsWith("--"))
                    {
                        bossSectionReached = fields[1].ToLower().Contains("boss");
                        isFileValid = true;
                        continue;
                    }

                    if (bossSectionReached)
                    {
                        uint[] events = new uint[2];
                        int index = 0;

                        foreach (var field in fields)
                        {
                            if (field.StartsWith("(#") && field.EndsWith(")"))
                            {
                                string _event = field.Substring(2).TrimEnd(')');
                                events.SetValue(uint.Parse(_event), index++);
                            }
                        }

                        if (index == 2)
                        {
                            eventToRdEvent.Add(events[0], events[1]);
                        }
                        else if (fields.Length > 1)
                        {
                            Console.WriteLine("Error in parsing: " + fields.ToString() + " at line: " + i);
                        }

                    }

                }
            }

            if (!isFileValid)
            {
                throw new FileFormatException("No Boss section found in the randomizer file");
            }

            // format existing dict to map string event to string event 
            if (eventToRdEvent.Count > 0)
            {
                // reverse eventToMemory: memory to list of event (multi bosses?)
                Dictionary<uint, List<string>> memoryToEvent = new Dictionary<uint, List<string>>();
                foreach (KeyValuePair<string, uint> entry in eventToMemory)
                {
                    if (memoryToEvent.ContainsKey(entry.Value))
                    {
                        memoryToEvent[entry.Value].Add(entry.Key);
                    }
                    else
                    {
                        memoryToEvent.Add(entry.Value, new List<string>() { entry.Key });
                    }
                }

                // create the event to event dict
                foreach (KeyValuePair<uint, List<string>> entry in memoryToEvent)
                {
                    if (eventToRdEvent.ContainsKey(entry.Key))
                    {
                        foreach (string oldEvent in entry.Value)
                        {
                            uint newMemory = eventToRdEvent[entry.Key];
                            if (memoryToEvent.ContainsKey(newMemory))
                            {
                                foreach (string newEvent in memoryToEvent[newMemory])
                                {
                                    if (!randomizerMapping.ContainsKey(oldEvent))
                                    {
                                        randomizerMapping.Add(oldEvent, newEvent);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Multiple events found for: " + oldEvent);
                                    }
                                }
                            }

                        }
                    }
                }

                applyRandomizedMapping();
            }
        }

        private void applyRandomizedMapping()
        {
            if (randomizerMapping.Count == 0)
            {
                return;
            }

            // Apply randomized mapping to initial_increment then copy it to IncrementMap
            Dictionary<string, int> newIncrement = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> entry in InitialIncrementMap)
            {
                string newKeyName = randomizerMapping.ContainsKey(entry.Key) ? randomizerMapping[entry.Key] : "@@@KEY_NOT_FOUND@@@";
                newIncrement.Add(entry.Key, InitialIncrementMap.ContainsKey(newKeyName) ? InitialIncrementMap[newKeyName] : entry.Value);
            }
            InitialIncrementMap = newIncrement;
            IncrementMap = new Dictionary<string, int>(newIncrement);
        }

        public void OutputIncrement(string csvPath)
        {
            using (var w = new StreamWriter(csvPath))
            {
                foreach (KeyValuePair<string, int> entry in DefaultPointConf)
                {
                    w.WriteLine(string.Format("{0},{1}", entry.Key, entry.Value));
                    w.Flush();
                }
            }

        }
    }

    public interface ICounter
    {
        int Count { get; }
        string lastPointsEarnedMsg { get; }

        Dictionary<string, int> IncrementMap { get; }

        bool Increment(IGame ERGame);
        void Reset();
        void SetCount(int value);
        void SetIncrement(string csvPath);
    }

}
