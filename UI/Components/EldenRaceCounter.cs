using Microsoft.VisualBasic.FileIO;
using SoulMemory;
using SoulMemory.EldenRing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.UI.Components
{
    public class EldenRaceCounter : ICounter
    {
        private int initialValue = 0;
        private Dictionary<string, int> initialIncrement = new Dictionary<string, int>();
        private Dictionary<string, int> increment= new Dictionary<string, int>();
        private Dictionary<string, uint> eventToMemory= new Dictionary<string, uint>();
        private List<Type> EventTypes = new List<Type>{ typeof(Boss), typeof(Grace), typeof(ItemPickup) };

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value for the counter.</param>
        /// <param name="increment">The amount to be used for incrementing/decrementing.</param>
        public EldenRaceCounter(int initialValue = 0)
        {
            this.initialValue = initialValue;
            Count = initialValue;
            SetEventToMemory();
        }

        public int Count { get; private set; }

        /// <summary>
        /// Increments this instance.
        /// <param name="ERGame">ER instance game to search event</param>
        /// </summary>
        public bool Increment(IGame ERGame)
        {
            if (Count == int.MaxValue)
                return false;

            List<string> popKeys = new List<string>();
            try
            {
                foreach (KeyValuePair<string, int> entry in increment)
                {
                    if (ERGame.ReadEventFlag(eventToMemory[entry.Key]))
                    {
                        Count = checked(Count + entry.Value);
                        popKeys.Add(entry.Key);
                    }
                }
            }
            catch (OverflowException)
            {
                Count = int.MaxValue;
                return false;
            }

            foreach(string key in popKeys)
            {
                increment.Remove(key);
            }

            return true;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            Count = initialValue;
            increment = new Dictionary<string, int>(initialIncrement);
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
        /// Sets the map value increment from eventName to points.
        /// </summary>
        /// <param name="csvPath"></param>
        public void SetIncrement(string csvPath)
        {
            increment = new Dictionary<string, int>();
            initialIncrement = new Dictionary<string, int>();

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
                        throw new Exception("Cannot read file " + csvPath + ",  error at line " + i);
                    }

                    string eventName = fields[0];
                    int eventPoints = int.Parse(fields[1]);


                    if (!eventToMemory.ContainsKey(eventName)){
                        throw new Exception("Unknown event: " + eventName + " at line " + i);
                    }

                    increment[eventName] = eventPoints;
                    initialIncrement[eventName] = eventPoints;

                }
            }
        }
    }

    public interface ICounter
    {
        int Count { get; }

        bool Increment(IGame ERGame);
        void Reset();
        void SetCount(int value);
        void SetIncrement(string csvPath);
    }
}
