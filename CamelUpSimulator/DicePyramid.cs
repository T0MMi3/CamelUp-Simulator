using System;
using System.Collections.Generic;

namespace CamelUpSimulator
{
    public class DicePyramid
    {
        public List<string> RemainingDice { get; private set; }

        public DicePyramid(List<string> camels)
        {
            RemainingDice = new List<string>(camels);
        }

        public string? Roll()
        {
            if (RemainingDice.Count == 0)
                return null;

            Random rnd = new Random();
            int index = rnd.Next(RemainingDice.Count);
            string die = RemainingDice[index];
            RemainingDice.RemoveAt(index);
            return die;
        }
    }
}
