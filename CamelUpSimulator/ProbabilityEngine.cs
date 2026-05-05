using System;
using System.Collections.Generic;
using System.Linq;

namespace CamelUpSimulator
{
    public class LegProbabilityResult
    {
        public Dictionary<string, double> FirstPlaceOdds { get; set; } = new();
        public Dictionary<string, double> SecondPlaceOdds { get; set; } = new();
        public int SimulationsRun { get; set; }
    }

    public static class ProbabilityEngine
    {
        public static LegProbabilityResult CalculateLegProbabilities(Game game, int simulations = 5000)
        {
            var result = new LegProbabilityResult();
            var rng = new Random();

            var normalColors = game.Board.Camels
                .Where(c => c.Color != "white" && c.Color != "black")
                .Select(c => c.Color)
                .Distinct()
                .ToList();

            var firstCounts = normalColors.ToDictionary(c => c, c => 0);
            var secondCounts = normalColors.ToDictionary(c => c, c => 0);

            for (int sim = 0; sim < simulations; sim++)
            {
                Board simBoard = CloneBoard(game.Board);
                List<string> remainingDice = game.DicePyramid.GetRemainingDice();

                while (remainingDice.Count > 0)
                {
                    int dieIndex = rng.Next(remainingDice.Count);
                    string dieColor = remainingDice[dieIndex];
                    remainingDice.RemoveAt(dieIndex);

                    int roll = rng.Next(1, 4); // 1, 2, or 3

                    if (dieColor == "grey")
                    {
                        SimulateGreyDie(simBoard, roll, rng);
                    }
                    else
                    {
                        var camel = simBoard.Camels.FirstOrDefault(c => c.Color == dieColor);
                        if (camel != null)
                        {
                            MoveCamelSim(simBoard, camel, roll);
                        }
                    }

                    if (IsRaceFinishedSim(simBoard))
                        break;
                }

                var order = simBoard.GetCamelOrder()
                    .Where(c => c != "white" && c != "black")
                    .ToList();

                if (order.Count > 0)
                    firstCounts[order[0]]++;

                if (order.Count > 1)
                    secondCounts[order[1]]++;
            }

            foreach (var color in normalColors)
            {
                result.FirstPlaceOdds[color] = 100.0 * firstCounts[color] / simulations;
                result.SecondPlaceOdds[color] = 100.0 * secondCounts[color] / simulations;
            }

            result.SimulationsRun = simulations;
            return result;
        }

        public static Board CloneBoard(Board original)
        {
            var clone = new Board(original.SpacesCount, new List<string>());

            clone.Camels.Clear();
            clone.DesertTiles.Clear();

            for (int i = 0; i < original.Spaces.Count; i++)
            {
                clone.Spaces[i].Clear();

                foreach (var camel in original.Spaces[i])
                {
                    var copiedCamel = new Camel(camel.Color, camel.Position)
                    {
                        StackHeight = camel.StackHeight
                    };

                    clone.Spaces[i].Add(copiedCamel);
                    clone.Camels.Add(copiedCamel);
                }
            }

            foreach (var tile in original.DesertTiles)
            {
                clone.DesertTiles.Add(new DesertTile(tile.OwnerName, tile.Position, tile.IsOasis));
            }

            return clone;
        }

        private static void SimulateGreyDie(Board board, int roll, Random rng)
        {
            var white = board.Camels.FirstOrDefault(c => c.Color == "white");
            var black = board.Camels.FirstOrDefault(c => c.Color == "black");

            if (white == null || black == null)
                return;

            Camel chosen;

            bool whiteHasRacers = board.Spaces[white.Position]
                .Any(c => c.Color != "white" && c.Color != "black" && c.StackHeight > white.StackHeight);

            bool blackHasRacers = board.Spaces[black.Position]
                .Any(c => c.Color != "white" && c.Color != "black" && c.StackHeight > black.StackHeight);

            if (white.Position == black.Position)
            {
                chosen = (white.StackHeight > black.StackHeight) ? white : black;
            }
            else if (whiteHasRacers && !blackHasRacers)
            {
                chosen = white;
            }
            else if (blackHasRacers && !whiteHasRacers)
            {
                chosen = black;
            }
            else
            {
                chosen = rng.Next(2) == 0 ? white : black;
            }

            MoveCrazyCamelSim(board, chosen, roll);
        }

        private static void MoveCamelSim(Board board, Camel camel, int distance)
        {
            if (camel == null)
                return;

            if (camel.Position < 0 || camel.Position >= board.SpacesCount)
                return;

            int oldPos = camel.Position;
            var oldStack = board.Spaces[oldPos];

            int camelIndex = oldStack.IndexOf(camel);
            if (camelIndex < 0)
                return;

            var movingCamels = oldStack.Skip(camelIndex).ToList();
            oldStack.RemoveRange(camelIndex, movingCamels.Count);

            int newPos = camel.Position + distance;

            if (newPos >= board.SpacesCount)
            {
                foreach (var c in movingCamels)
                    c.Position = board.SpacesCount;

                return;
            }

            var tile = board.DesertTiles.FirstOrDefault(t => t.Position == newPos);
            if (tile != null)
            {
                int shift = tile.IsOasis ? 1 : -1;
                newPos = Math.Max(0, Math.Min(board.SpacesCount - 1, newPos + shift));
            }

            var destStack = board.Spaces[newPos];
            destStack.AddRange(movingCamels);

            for (int i = 0; i < destStack.Count; i++)
            {
                destStack[i].Position = newPos;
                destStack[i].StackHeight = i;
            }
        }

        private static void MoveCrazyCamelSim(Board board, Camel camel, int distance)
        {
            if (camel == null)
                return;

            if (camel.Position < 0 || camel.Position >= board.SpacesCount)
                return;

            int oldPos = camel.Position;
            var oldStack = board.Spaces[oldPos];

            int camelIndex = oldStack.IndexOf(camel);
            if (camelIndex < 0)
                return;

            var movingCamels = oldStack.Skip(camelIndex).ToList();
            oldStack.RemoveRange(camelIndex, movingCamels.Count);

            int newPos = Math.Max(0, camel.Position - distance);

            var tile = board.DesertTiles.FirstOrDefault(t => t.Position == newPos);
            if (tile != null)
            {
                int shift = tile.IsOasis ? -1 : 1; // reversed for crazy camels
                newPos = Math.Max(0, Math.Min(board.SpacesCount - 1, newPos + shift));
            }

            var destStack = board.Spaces[newPos];
            bool hasNormalAtDest = destStack.Any(c => c.Color != "white" && c.Color != "black");

            if (hasNormalAtDest)
                destStack.InsertRange(0, movingCamels);
            else
                destStack.AddRange(movingCamels);

            for (int i = 0; i < destStack.Count; i++)
            {
                destStack[i].Position = newPos;
                destStack[i].StackHeight = i;
            }
}

        public static List<DesertTileRecommendation> EvaluateDesertTilePlacements(Game game, Player player, int simulations = 3000)
        {
            var results = new List<DesertTileRecommendation>();

            var baselineProbs = CalculateLegProbabilities(game, simulations);
            double currentPortfolioEV = game.GetPlayerLegPortfolioEV(player, baselineProbs);

            var legalPositions = game.GetLegalDesertTilePositionsForPlayer(player);

            foreach (int pos in legalPositions)
            {
                foreach (bool isCheering in new[] { true, false })
                {
                    // Clone board
                    Board simBoard = CloneBoard(game.Board);

                    // Place hypothetical tile
                    simBoard.PlaceDesertTile(new DesertTile(player.Name, pos, isCheering));

                    // Create simulated game
                    Game simGame = game.CloneForSimulation(simBoard);

                    // Recalculate probabilities
                    var probs = CalculateLegProbabilities(simGame, simulations);

                    double newPortfolioEV = simGame.GetPlayerLegPortfolioEV(player, probs);
                    double evGain = newPortfolioEV - currentPortfolioEV;
                    double hitProbability = 0; // placeholder for now

                    results.Add(new DesertTileRecommendation
                    {
                        Position = pos,
                        IsCheering = isCheering,
                        PortfolioEVBefore = currentPortfolioEV,
                        PortfolioEVAfter = newPortfolioEV,
                        TileHitProbability = 0, // placeholder for now
                        TotalScore = evGain + hitProbability
                    });
                }
            }

            return results
                .OrderByDescending(r => r.TotalScore)
                .ToList();
        }

        private static bool IsRaceFinishedSim(Board board)
        {
            return board.Camels
                .Where(c => c.Color != "white" && c.Color != "black")
                .Any(c => c.Position >= board.SpacesCount);
        }
    }
}