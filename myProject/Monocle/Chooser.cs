using System;
using System.Collections.Generic;
using System.Globalization;

namespace Monocle
{
    public class Chooser<T>
    {
        private List<Choice> choices;

        public Chooser()
        {
            choices = new List<Choice>();
        }

        public Chooser(T firstChoice, float weight)
            : this()
        {
            Add(firstChoice, weight);
        }

        public Chooser(params T[] choices)
            : this()
        {
            foreach (T choice in choices)
                Add(choice, 1);
        }

        public int Count
        {
            get { return choices.Count; }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();
                return choices[index].Value;
            }

            set
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();
                choices[index].Value = value;
            }
        }

        public Chooser<T> Add(T choice, float weight)
        {
            weight = Math.Max(weight, 0);
            choices.Add(new Choice(choice, weight));
            TotalWeight += weight;
            return this;
        }

        public T Choose()
        {
            if (TotalWeight <= 0)
                return default;
            else if (choices.Count == 1)
                return choices[0].Value;

            double roll = Calc.Random.NextDouble() * TotalWeight;
            float check = 0;

            for (int i = 0; i < choices.Count - 1; i++)
            {
                check += choices[i].Weight;
                if (roll < check)
                    return choices[i].Value;
            }
            return choices[choices.Count - 1].Value;
        }

        public float TotalWeight { get; private set; }

        public bool CanChoose
        {
            get { return TotalWeight > 0; }
        }

        /// <summary>
        /// Parses a chooser from a string, formatted as: "0:3,1:1.5,2" (item:weight, with weight defaulting to 1)
        /// </summary>
        public static Chooser<TT> FromString<TT>(string data) where TT : IConvertible
        {
            Chooser<TT> chooser = new Chooser<TT>();
            string[] entries = data.Split(',');

            // single entry, no weight specified
            if (entries.Length == 1 && entries[0].IndexOf(':') == -1)
            {
                chooser.Add((TT)Convert.ChangeType(entries[0], typeof(TT)), 1);
                return chooser;
            }

            foreach (string entry in entries)
            {
                if (entry.IndexOf(':') == -1)
                    chooser.Add((TT)Convert.ChangeType(entry, typeof(TT)), 1);
                else
                {
                    string[] parts = entry.Split(':');
                    string value = parts[0].Trim();
                    string weight = parts[1].Trim();

                    chooser.Add((TT)Convert.ChangeType(value, typeof(TT)), Convert.ToSingle(weight, CultureInfo.InvariantCulture));
                }
            }

            return chooser;
        }

        private class Choice
        {
            public T Value;
            public float Weight;

            public Choice(T value, float weight)
            {
                Value = value;
                Weight = weight;
            }
        }
    }
}
