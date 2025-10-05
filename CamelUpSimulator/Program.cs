using System;
using CamelUpSimulator;

class Program
{
    static void Main(string[] args)
    {
        // Initialize camels
        Camel[] normalCamels = {
            new Camel("Blue"),
            new Camel("Green"),
            new Camel("Red"),
            new Camel("Yellow"),
            new Camel("Purple")
        };

        Camel[] crazyCamels = {
            new Camel("Black", movesForward: false),
            new Camel("White", movesForward: false)
        };

        // Initialize board here to avoid "use of unassigned variable" warning
        Board board = new Board();  
        bool positionsConfirmed = false;

        while (!positionsConfirmed)
        {
            // Reset board
            board = new Board();

            // Get starting positions for normal camels
            foreach (var camel in normalCamels)
            {
                int position = GetStartingPosition(camel.Color, true);
                camel.Position = position;
                board.AddCamel(camel);
            }

            // Get starting positions for crazy camels
            foreach (var camel in crazyCamels)
            {
                int position = GetStartingPosition(camel.Color, false);
                camel.Position = position;
                board.AddCamel(camel);
            }

            // Show board
            Console.WriteLine("\nCurrent board positions:");
            board.PrintBoard();

            // Ask for confirmation
            Console.Write("\nAre these positions correct? (y/n): ");
            string confirm = (Console.ReadLine() ?? "").Trim().ToLower();
            if (confirm == "y" || confirm == "yes")
                positionsConfirmed = true;
            else
                Console.WriteLine("Let's re-enter positions.\n");
        }

        Console.WriteLine("\nFinal starting board:");
        board.PrintBoard();

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    // Get starting position for a camel with validation
    static int GetStartingPosition(string color, bool isNormal)
    {
        int position = -1;

        while (true)
        {
            Console.Write($"Enter starting position for {color} " +
                          $"({(isNormal ? "1-3" : "14-16")}): ");

            string input = Console.ReadLine() ?? "";

            if (!int.TryParse(input, out position))
            {
                Console.WriteLine("Invalid input. Enter a number.");
                continue;
            }

            if (isNormal && (position < 1 || position > 3))
            {
                Console.WriteLine("Normal camels must start on spaces 1-3.");
                continue;
            }

            if (!isNormal && (position < 14 || position > 16))
            {
                Console.WriteLine("Crazy camels must start on spaces 14-16.");
                continue;
            }

            break;
        }

        return position;
    }
}
