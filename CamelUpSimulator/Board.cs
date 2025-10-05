using System;
using System.Collections.Generic;
using System.Linq;

namespace CamelUpSimulator
{
    public class Board
    {
        public int SpacesCount { get; } = 16;

        // Each space holds a stack of camels (bottom to top)
        public List<List<Camel>> Spaces { get; set; }

        public Board()
        {
            Spaces = new List<List<Camel>>();
            for (int i = 0; i < SpacesCount; i++)
                Spaces.Add(new List<Camel>());
        }

        // Add a camel to a space
        public void AddCamel(Camel camel)
        {
            if (camel.Position < 1 || camel.Position > SpacesCount)
                throw new ArgumentOutOfRangeException(nameof(camel.Position), 
                    "Camel position must be between 1 and 16");

            // Set height based on existing stack
            camel.Height = Spaces[camel.Position - 1].Count;

            // Add camel to that space
            Spaces[camel.Position - 1].Add(camel);
        }

        // Remove a camel (and all camels stacked above it)
        public List<Camel> RemoveCamelStack(Camel camel)
        {
            var stack = Spaces[camel.Position - 1];
            int index = stack.IndexOf(camel);

            if (index == -1)
                throw new InvalidOperationException("Camel not found in expected position");

            // Remove camel and all above it
            var movingStack = stack.Skip(index).ToList();
            stack.RemoveRange(index, movingStack.Count);

            return movingStack;
        }

        // Place a stack of camels onto a space (maintaining order)
        public void PlaceStack(List<Camel> camels, int newPosition)
        {
            if (newPosition < 1)
                newPosition = 1;
            if (newPosition > SpacesCount)
                newPosition = SpacesCount;

            var stack = Spaces[newPosition - 1];
            // Update heights
            for (int i = 0; i < camels.Count; i++)
                camels[i].Height = stack.Count + i;

            stack.AddRange(camels);

            // Update camel positions
            foreach (var c in camels)
                c.Position = newPosition;
        }

        // Print board for debugging
        public void PrintBoard()
        {
            for (int i = 0; i < SpacesCount; i++)
            {
                Console.Write($"Space {i + 1}: ");
                if (Spaces[i].Count == 0)
                    Console.WriteLine("[empty]");
                else
                {
                    foreach (var camel in Spaces[i])
                        Console.Write($"{camel.Color} ");
                    Console.WriteLine();
                }
            }
        }
    }
}
