using System;
using System.Collections.Generic;
using System.Linq;

namespace CamelUpSimulator
{
    public class DicePyramid
    {
        // All dice that exist in a leg: normal camel colors + a single grey die
        private readonly List<string> allDice;

        // Dice still available to roll in the current leg
        private List<string> remainingDice;

        // How many pyramid tickets have been used this leg (1 per roll, max 5)
        public int PyramidTicketsUsed { get; private set; }
        // Public read-only view of dice that haven't been rolled yet
        public IReadOnlyList<string> RemainingDice => remainingDice.AsReadOnly();


        public DicePyramid(IEnumerable<string> camelColors)
        {
            if (camelColors == null) throw new ArgumentNullException(nameof(camelColors));

            // Only NORMAL camel dice are in the pyramid (exclude crazy camels)
            var normalColors = camelColors
                .Where(c => c != null)
                .Select(c => c.ToLower())
                .Where(c => c != "white" && c != "black")
                .Distinct()
                .ToList();

            allDice = new List<string>(normalColors);

            // One grey die for the pair of crazy camels
            allDice.Add("grey");

            remainingDice = new List<string>(allDice);
            PyramidTicketsUsed = 0;
        }

        /// <summary>
        /// Returns a copy of the remaining dice list for safe iteration/printing.
        /// </summary>
        public List<string> GetRemainingDice() => new List<string>(remainingDice);

        /// <summary>
        /// True if the die is still available this leg.
        /// </summary>
        public bool IsDieAvailable(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return false;
            return remainingDice.Contains(color.ToLower());
        }

        /// <summary>
        /// Consume a die (mark as used) and increment the pyramid ticket count.
        /// This should be called exactly once per roll, after the move is applied.
        /// </summary>
        public void UseDie(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return;
            color = color.ToLower();

            if (remainingDice.Contains(color))
            {
                remainingDice.Remove(color);
            }

            // Each roll consumes one pyramid ticket (max 5 per leg)
            if (PyramidTicketsUsed < 5)
                PyramidTicketsUsed++;
        }

        /// <summary>
        /// Reset for the next leg: restore dice and zero ticket counter.
        /// </summary>
        public void ResetDice()
        {
            remainingDice = new List<string>(allDice);
            PyramidTicketsUsed = 0;
        }
    }
}
