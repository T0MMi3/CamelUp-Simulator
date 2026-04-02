using System;
using System.Collections.Generic;
using System.Linq;

namespace CamelUpSimulator
{
    public class Board
    {
        public List<Camel> Camels { get; private set; } = new List<Camel>();
        public List<DesertTile> DesertTiles { get; private set; } = new List<DesertTile>();
        public int SpacesCount { get; private set; } = 16;
        public List<List<Camel>> Spaces { get; private set; }
        public List<Camel> CrazyCamels { get; private set; } = new List<Camel>();
        public List<LegBet> LegBets { get; set; } = new List<LegBet>();

        public Board(int boardSize, IEnumerable<string> camelColors)
        {
            Spaces = new List<List<Camel>>();
            for (int i = 0; i < boardSize; i++)
                Spaces.Add(new List<Camel>());

            Camels = new List<Camel>();

            foreach (var color in camelColors)
            {
                Camel camel = new Camel(color);
                Spaces[0].Add(camel);
                Camels.Add(camel);
            }
        }

        // ------------------------------------------------------------
        // Move normal camel
        // ------------------------------------------------------------
        public void MoveCamel(Camel camel, int distance, Game game)
        {
            int oldPos = camel.Position;
            var oldStack = Spaces[oldPos];
            int camelIndex = oldStack.IndexOf(camel);
            var movingCamels = oldStack.Skip(camelIndex).ToList();
            oldStack.RemoveRange(camelIndex, movingCamels.Count);

            bool isCrazy = camel.Color == "white" || camel.Color == "black";

            // Allow normal camels to move past the finish line
            int newPos = isCrazy
                ? Math.Max(0, camel.Position - distance)
                : camel.Position + distance;

            // Race-end guard — if beyond board, treat as finished
            if (!isCrazy && newPos >= SpacesCount)
            {
                foreach (var c in movingCamels)
                {
                    c.Position = SpacesCount; // logical tile just past finish
                }

                Console.WriteLine($"{camel.Color} crosses the finish line!");
                // No stacking or desert tile interaction beyond finish
                return;
            }

            // Desert tile interaction (normal board range only)
            var tile = DesertTiles.FirstOrDefault(t => t.Position == newPos);
            if (tile != null)
            {
                int shift = tile.IsOasis ? 1 : -1;
                if (isCrazy) shift *= -1; // reverse for crazy camels

                int adjustedPos = Math.Max(0, Math.Min(SpacesCount - 1, newPos + shift));

                string direction = shift > 0 ? "forward" : "backward";
                Console.WriteLine($"{camel.Color} landed on {tile.OwnerName}'s {(tile.IsOasis ? "Cheering (+1)" : "Booing (-1)")} tile and moves {direction} to {adjustedPos + 1}.");

                newPos = adjustedPos;

                var owner = game.Players.FirstOrDefault(p => p.Name == tile.OwnerName);
                if (owner != null)
                {
                    owner.AddPoints(1);
                    Console.WriteLine($"{owner.Name} gains +1 point from their tile!");
                }
            }

            // Make sure destination index is valid (0–SpacesCount-1)
            newPos = Math.Max(0, Math.Min(newPos, SpacesCount - 1));

            // Stacking behavior
            var destStack = Spaces[newPos];
            bool hasNormalHere = destStack.Any(c => c.Color != "white" && c.Color != "black");

            if (isCrazy)
            {
                if (hasNormalHere)
                    destStack.InsertRange(0, movingCamels);
                else
                    destStack.AddRange(movingCamels);
            }
            else
            {
                destStack.AddRange(movingCamels);
            }

            foreach (var c in movingCamels)
            {
                c.Position = newPos;
                c.StackHeight = destStack.IndexOf(c);
                if (!Camels.Contains(c))
                    Camels.Add(c);
            }

            Console.WriteLine($"{camel.Color} moves from {oldPos + 1} to {newPos + 1}.");
            PrintBoard();
        }


        // ------------------------------------------------------------
        // Official crazy camel logic (auto-selection + movement)
        // ------------------------------------------------------------
        public void HandleGreyDie(Game game, int rolledNumber, string? manualColorInput = null)
        {
            var white = Camels.FirstOrDefault(c => c.Color == "white");
            var black = Camels.FirstOrDefault(c => c.Color == "black");

            if (white == null || black == null)
            {
                Console.WriteLine("[DEBUG] One or both crazy camels missing.");
                return;
            }

            string chosenColor = manualColorInput ?? "";

            // ------------------------------
            // Gather current stack situation
            // ------------------------------
            bool whiteBelowBlack = (white.Position == black.Position) && (white.StackHeight < black.StackHeight);
            bool blackBelowWhite = (white.Position == black.Position) && (black.StackHeight < white.StackHeight);

            bool whiteHasRacers = Spaces[white.Position]
                .Any(c => c.Color != "white" && c.Color != "black" && c.StackHeight > white.StackHeight);
            bool blackHasRacers = Spaces[black.Position]
                .Any(c => c.Color != "white" && c.Color != "black" && c.StackHeight > black.StackHeight);

            // ---------------------------------------------------
            // RULE A: both crazy camels stacked together
            // ---------------------------------------------------
            if (white.Position == black.Position)
            {
                chosenColor = (white.StackHeight > black.StackHeight) ? "white" : "black";
                Console.WriteLine($"[Crazy Camel Rule A] Both are stacked. Top one ({chosenColor}) moves {rolledNumber} backward and carries the other.");
                MoveCrazyCamel(chosenColor == "white" ? white : black, rolledNumber, game);
                return;
            }

            // ---------------------------------------------------
            // RULE B: one has racers riding it
            // ---------------------------------------------------
            if (whiteHasRacers && !blackHasRacers)
            {
                Console.WriteLine($"[Crazy Camel Rule B] White has racers riding! White moves {rolledNumber} backward.");
                MoveCrazyCamel(white, rolledNumber, game);
                return;
            }
            if (blackHasRacers && !whiteHasRacers)
            {
                Console.WriteLine($"[Crazy Camel Rule B] Black has racers riding! Black moves {rolledNumber} backward.");
                MoveCrazyCamel(black, rolledNumber, game);
                return;
            }
            if (whiteHasRacers && blackHasRacers)
            {
                chosenColor = (white.StackHeight > black.StackHeight) ? "white" : "black";
                Console.WriteLine($"[Crazy Camel Rule B] Both have racers (weird case). Top one ({chosenColor}) moves {rolledNumber} backward.");
                MoveCrazyCamel(chosenColor == "white" ? white : black, rolledNumber, game);
                return;
            }

            // ---------------------------------------------------
            // RULE C: normal case — neither stacked nor carrying racers
            // ---------------------------------------------------
            if (string.IsNullOrWhiteSpace(chosenColor))
            {
                Console.Write("Grey die rolled! Which crazy camel moved? (white/black): ");
                chosenColor = Console.ReadLine()?.Trim().ToLower() ?? "white";
            }

            MoveCrazyCamel(chosenColor == "black" ? black : white, rolledNumber, game);
        }



        // ------------------------------------------------------------
        // Move a crazy camel backward (counterclockwise)
        // ------------------------------------------------------------
        public void MoveCrazyCamel(Camel camel, int distance, Game game)
        {
            if (camel == null || (camel.Color != "white" && camel.Color != "black"))
            {
                Console.WriteLine("[DEBUG] Tried to move non-crazy camel with MoveCrazyCamel().");
                return;
            }

            int oldPos = camel.Position;
            if (oldPos < 0 || oldPos >= Spaces.Count)
            {
                Console.WriteLine("[DEBUG] Invalid camel position!");
                return;
            }

            var oldStack = Spaces[oldPos];
            int camelIndex = oldStack.IndexOf(camel);
            if (camelIndex == -1)
            {
                Console.WriteLine("[DEBUG] Crazy camel not found in stack.");
                return;
            }

            // Move camel + anything on top of it (including racers)
            var movingCamels = oldStack.Skip(camelIndex).ToList();
            oldStack.RemoveRange(camelIndex, movingCamels.Count);

            int newPos = Math.Max(0, camel.Position - distance);

            // Desert tile reversed logic
            var tile = DesertTiles.FirstOrDefault(t => t.Position == newPos);
            if (tile != null)
            {
                int shift = tile.IsOasis ? -1 : 1; // reversed
                int adjustedPos = Math.Max(0, Math.Min(SpacesCount - 1, newPos + shift));

                Console.WriteLine($"{camel.Color} hits {tile.OwnerName}'s {(tile.IsOasis ? "Cheering (+1)" : "Booing (-1)")} tile (reversed) and moves to {adjustedPos + 1}.");
                newPos = adjustedPos;

                var owner = game.Players.FirstOrDefault(p => p.Name == tile.OwnerName);
                if (owner != null)
                {
                    owner.AddPoints(1);
                    Console.WriteLine($"{owner.Name} gains +1 point from their tile!");
                }
            }

            // Insert under normal camels if destination has racers
            var destStack = Spaces[newPos];
            bool hasNormalAtDest = destStack.Any(c => c.Color != "white" && c.Color != "black");

            if (hasNormalAtDest)
                destStack.InsertRange(0, movingCamels);
            else
                destStack.AddRange(movingCamels);

            foreach (var c in movingCamels)
            {
                c.Position = newPos;
                c.StackHeight = destStack.IndexOf(c);
            }

            Console.WriteLine($"{camel.Color} moves backward from {oldPos + 1} to {newPos + 1}.");
            PrintBoard();
        }



        // ------------------------------------------------------------
        // Utility / board display
        // ------------------------------------------------------------
        public bool PlaceDesertTile(DesertTile tile)
        {
            if (DesertTiles.Any(t => t.Position == tile.Position))
                return false;

            DesertTiles.Add(tile);
            return true;
        }

        public List<string> GetCamelOrder()
        {
            var normalCamels = Camels
                .Where(c => c.Color != "white" && c.Color != "black")
                .OrderByDescending(c => c.Position)
                .ThenByDescending(c => c.StackHeight)
                .Select(c => c.Color)
                .ToList();

            var crazyCamels = Camels
                .Where(c => c.Color == "white" || c.Color == "black")
                .OrderBy(c => c.Position)
                .ThenBy(c => c.StackHeight)
                .Select(c => c.Color)
                .ToList();

            return normalCamels.Concat(crazyCamels).ToList();
        }

        public void PrintBoard()
        {
            Console.WriteLine("Board:");
            int maxStackHeight = Spaces.Max(s => s.Count);

            for (int row = maxStackHeight - 1; row >= 0; row--)
            {
                for (int pos = 0; pos < SpacesCount; pos++)
                {
                    var stack = Spaces[pos];
                    if (stack.Count > row)
                        Console.Write(stack[row].Color[0] + "   ");
                    else
                        Console.Write(".   ");
                }
                Console.WriteLine();
            }

            for (int pos = 1; pos <= SpacesCount; pos++)
                Console.Write(pos.ToString().PadRight(4));
            Console.WriteLine();

            // PUT THIS RIGHT HERE 
            Console.WriteLine("    " + new string(' ', (SpacesCount - 1) * 4) + "FINISH!");

            for (int pos = 0; pos < SpacesCount; pos++)
            {
                var tile = DesertTiles.FirstOrDefault(t => t.Position == pos);
                if (tile != null)
                    Console.Write($"[{(tile.IsOasis ? "C" : "B")}] ");
                else
                    Console.Write("    ");
            }
            Console.WriteLine("\n");
        }


        public void ResetDesertTiles() => DesertTiles.Clear();
    }
}
