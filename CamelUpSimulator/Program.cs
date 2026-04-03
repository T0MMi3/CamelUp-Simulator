using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CamelUpSimulator;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to Camel Up Simulator!");
        Console.WriteLine("This simulator tracks the REAL game state for AI move suggestions.\n");

        // ----------------------------
        // Player setup
        // ----------------------------
        Console.Write("Enter number of players: ");
        int numPlayers;
        while (!int.TryParse(Console.ReadLine(), out numPlayers) || numPlayers < 1)
            Console.Write("Invalid input. Enter a positive number of players: ");

        List<string> playerNames = new();
        for (int i = 0; i < numPlayers; i++)
        {
            Console.Write($"Enter name for player {i + 1}: ");
            string? name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) name = $"Player{i + 1}";
            playerNames.Add(name);
        }

        // ----------------------------
        // Board / camel setup
        // ----------------------------
        List<string> normalCamels = new() { "blue", "green", "red", "yellow", "purple" };
        List<string> specialCamels = new() { "white", "black" };

        bool confirmed = false;
        Board preSetupBoard = null!;

        while (!confirmed)
        {
            preSetupBoard = new Board(16, new List<string>());

            Console.WriteLine("\n=== Setup: Place Normal Camels (tiles 1-3) ===");
            List<string> remainingNormal = new(normalCamels);

            for (int pos = 0; pos <= 2; pos++)
            {
                List<string> camelsOnTile = new();
                bool adding = true;
                while (adding)
                {
                    Console.WriteLine($"\nTile {pos + 1} - Current stack (bottom -> top): {string.Join(",", camelsOnTile)}");
                    Console.WriteLine($"Available camels to place: {string.Join(", ", remainingNormal)}");
                    Console.Write("Enter camel color to place next on stack (or 'd' to finish this tile): ");
                    string? camel = Console.ReadLine()?.Trim().ToLower();
                    if (string.IsNullOrEmpty(camel))
                    {
                        Console.WriteLine("Invalid input.");
                        continue;
                    }

                    if (camel == "d") adding = false;
                    else if (remainingNormal.Contains(camel))
                    {
                        Camel c = new Camel(camel) { Position = pos };
                        preSetupBoard.Spaces[pos].Add(c);
                        preSetupBoard.Camels.Add(c);
                        camelsOnTile.Add(camel);
                        remainingNormal.Remove(camel);
                    }
                    else Console.WriteLine("Invalid or already placed camel. Try again.");
                }
            }

            Console.WriteLine("\n=== Setup: Place White/Black Camels (tiles 14-16) ===");
            List<string> remainingSpecial = new(specialCamels);

            for (int pos = 13; pos <= 15; pos++)
            {
                List<string> camelsOnTile = new();
                bool adding = true;
                while (adding)
                {
                    Console.WriteLine($"\nTile {pos + 1} - Current stack (bottom -> top): {string.Join(",", camelsOnTile)}");
                    Console.WriteLine($"Available camels to place: {string.Join(", ", remainingSpecial)}");
                    Console.Write("Enter camel color to place next on stack (or 'd' to finish this tile): ");
                    string? camel = Console.ReadLine()?.Trim().ToLower();
                    if (string.IsNullOrEmpty(camel))
                    {
                        Console.WriteLine("Invalid input.");
                        continue;
                    }

                    if (camel == "d") adding = false;
                    else if (remainingSpecial.Contains(camel))
                    {
                        Camel c = new Camel(camel) { Position = pos };
                        preSetupBoard.Spaces[pos].Add(c);

                        // Add to both lists for tracking
                        preSetupBoard.Camels.Add(c);
                        preSetupBoard.CrazyCamels.Add(c);

                        camelsOnTile.Add(camel);
                        remainingSpecial.Remove(camel);
                    }
                    else Console.WriteLine("Invalid or already placed camel. Try again.");
                }
            }

            Console.WriteLine("\n=== Setup Complete ===");
            preSetupBoard.PrintBoard();

            bool allPlaced =
                normalCamels.All(c => preSetupBoard.Camels.Any(x => x.Color == c)) &&
                specialCamels.All(c => preSetupBoard.Camels.Any(x => x.Color == c));

            if (!allPlaced)
            {
                Console.WriteLine("\nNot all camels have been placed! Please redo setup.");
                continue;
            }

            Console.Write("\nDoes this board match your real game setup? (Y/N): ");
            string confirm = Console.ReadLine()?.Trim().ToUpper() ?? "";
            confirmed = (confirm == "Y");
        }

        // ----------------------------
        // Game initialization
        // ----------------------------
        Game game = new(playerNames, preSetupBoard);

        Console.WriteLine("\nStarting game tracking...");
        game.Board.PrintBoard();

        // ----------------------------
        // Run main async loop (Game.cs controls leg flow)
        // ----------------------------
        await game.PlayGameAsync();

        // ----------------------------
        // Game over
        // ----------------------------
        Console.WriteLine("\nGame over! Final scores:");
        foreach (var player in game.Players)
            Console.WriteLine($"{player.Name}: {player.TotalPoints} points");

        Console.WriteLine("\nPress ENTER to exit.");
        Console.ReadLine();
    }
}
