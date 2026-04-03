using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CamelUpSimulator
{
    public class Game
    {
        public Board Board { get; private set; }
        public List<Player> Players { get; private set; }
        public DicePyramid DicePyramid { get; private set; }
        public int CurrentLeg { get; private set; }
        public string CurrentAiSuggestion { get; set; } = "";
        public Guid GameId { get; } = Guid.NewGuid();

        // Public final-pile info visible during live play
        private List<Player> winnerPilePlayers = new();
        private List<Player> loserPilePlayers = new();

        public int TotalWinnerPileBets => winnerPilePlayers.Count;
        public int TotalLoserPileBets => loserPilePlayers.Count;

        // Turn order: the player who starts the current leg
        private int startingPlayerIndex = 0;

        // Hidden final bet details entered only at end of game
        // Order matters: bottom first, top last
        private List<(Player player, string color)> hiddenWinnerPile = new();
        private List<(Player player, string color)> hiddenLoserPile = new();

        // Leg bet decks
        private Dictionary<string, Stack<LegBet>> legBetDecks = new();

        public Game(List<string> playerNames, Board preSetupBoard)
        {
            Board = preSetupBoard;
            Players = playerNames.Select(name => new Player(name)).ToList();
            DicePyramid = new DicePyramid(Board.Camels.Select(c => c.Color).ToList());
            CurrentLeg = 1;

            InitializeLegBets();

            Console.WriteLine($"[DEBUG] Game ID: {GameId}");
        }

        // ----------------------------
        // Core game loop
        // ----------------------------
        public async Task PlayGameAsync()
        {
            Console.WriteLine("Starting Camel Up Simulator!");

            while (!IsRaceFinished())
            {
                Console.WriteLine($"\n=== Leg {CurrentLeg} ===");

                var startingPlayer = GetStartingPlayer();
                int startIndex = Players.IndexOf(startingPlayer);
                Console.WriteLine($"[DEBUG] Leg {CurrentLeg} will start with {startingPlayer.Name}.");

                bool legOver = false;

                for (int i = 0; i < Players.Count; i++)
                {
                    var player = Players[(startIndex + i) % Players.Count];

                    Console.WriteLine($"\n{player.Name}'s turn:");
                    Board.PrintBoard();

                    await player.TakeTurnAsync(this);

                    // Leg ends when all 5 dice are used
                    if (DicePyramid.PyramidTicketsUsed >= 5)
                    {
                        Console.WriteLine("\n--- All 5 pyramid tickets taken. Ending leg... ---");

                        // Next leg starts to the left of the player who ended this leg
                        startingPlayerIndex = (Players.IndexOf(player) + 1) % Players.Count;
                        Console.WriteLine($"[DEBUG] Next leg will start with {Players[startingPlayerIndex].Name}.");

                        EndLegScoring();
                        legOver = true;
                        break;
                    }

                    // Race ends when a normal camel moves beyond space 16
                    if (IsRaceFinished())
                    {
                        Console.WriteLine("\n🏁 A camel has crossed the finish line! Ending the leg and race...");

                        // This would be the next starter if another leg happened
                        startingPlayerIndex = (Players.IndexOf(player) + 1) % Players.Count;
                        Console.WriteLine($"[DEBUG] Next leg would start with {Players[startingPlayerIndex].Name}.");

                        EndLegScoring();
                        legOver = true;
                        break;
                    }
                }

                if (IsRaceFinished())
                    break;

                if (legOver)
                    CurrentLeg++;
            }

            EndFinalScoring();
            LogGameResult();
        }

        public Player GetStartingPlayer() => Players[startingPlayerIndex];

        // ----------------------------
        // Race state helpers
        // ----------------------------
        public bool IsRaceFinished()
        {
            int finishIndex = Board.Spaces.Count - 1;

            // A normal camel must move BEYOND the last board space to finish
            return Board.Camels
                .Where(c => c.Color != "white" && c.Color != "black")
                .Any(c => c.Position > finishIndex);
        }

        public void GrantPyramidTicket(Player player)
        {
            if (player == null) return;

            player.AddPoints(1);
            player.UsePyramidTicket();
        }

        // ----------------------------
        // Final bet public tracking
        // ----------------------------
        public void RecordFinalPileBet(Player player, string pile)
        {
            if (pile == "winner")
                winnerPilePlayers.Add(player);
            else if (pile == "loser")
                loserPilePlayers.Add(player);

            Console.WriteLine($"[DEBUG] Final pile totals -> Winner: {winnerPilePlayers.Count}, Loser: {loserPilePlayers.Count}");
        }

        // ----------------------------
        // Leg bet deck setup / access
        // ----------------------------
        private void InitializeLegBets()
        {
            legBetDecks.Clear();

            var normalColors = Board.Camels
                .Where(c => c.Color != "white" && c.Color != "black")
                .Select(c => c.Color)
                .Distinct()
                .ToList();

            foreach (var color in normalColors)
            {
                var values = new List<int> { 5, 3, 2, 2 };
                legBetDecks[color] = new Stack<LegBet>(
                    values.Select(v => new LegBet(color, v)).Reverse()
                );
            }
        }

        public void ResetLegBets()
        {
            InitializeLegBets();

            foreach (var player in Players)
                player.HeldLegBets.Clear();

            Console.WriteLine("[DEBUG] Leg bet decks reset for new leg.");
        }

        public List<string> AvailableLegBetColors()
        {
            return legBetDecks
                .Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public LegBet? TakeLegBetCard(string color)
        {
            if (!legBetDecks.ContainsKey(color))
                return null;

            var stack = legBetDecks[color];

            if (stack.Count == 0)
                return null;

            return stack.Pop();
        }

        public void DisplayLegBetStatus()
        {
            Console.WriteLine("\nCurrent Leg Bet Cards:");
            foreach (var kvp in legBetDecks)
            {
                var color = kvp.Key;
                var cards = kvp.Value.ToList();

                if (cards.Count == 0)
                    Console.WriteLine($"  {color,-8}: [None left]");
                else
                    Console.WriteLine($"  {color,-8}: Top = {cards.First().Value} pts | Remaining = {cards.Count}");
            }
        }

        // ----------------------------
        // End of leg
        // ----------------------------
        public void EndLegScoring()
        {
            var camelOrderFull = Board.GetCamelOrder();
            var camelOrder = camelOrderFull
                .Where(c => c != "white" && c != "black")
                .ToList();

            Console.WriteLine("\n================= END OF LEG =================");
            Console.WriteLine("Camel order (normal camels only): " + string.Join(", ", camelOrder));
            Console.WriteLine($"[DEBUG] EndLegScoring() called for Leg {CurrentLeg}");

            foreach (var player in Players)
                player.ScoreLegBets(camelOrder);

            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Updated Player Scores:");
            foreach (var player in Players)
                Console.WriteLine($"{player.Name}: {player.TotalPoints} points");

            DicePyramid.ResetDice();
            Board.ResetDesertTiles();

            foreach (var player in Players)
                player.ResetDesertTile();

            ResetLegBets();

            Console.WriteLine("\n--- Leg complete. Press ENTER to continue. ---");
            Console.ReadLine();
        }

        // ----------------------------
        // Hidden final bet entry
        // ----------------------------
        public void AddHiddenWinnerBet(Player player, string color)
        {
            hiddenWinnerPile.Add((player, color));
        }

        public void AddHiddenLoserBet(Player player, string color)
        {
            hiddenLoserPile.Add((player, color));
        }

        public void EnterHiddenFinalBets()
        {
            hiddenWinnerPile.Clear();
            hiddenLoserPile.Clear();

            Console.WriteLine("\n=== Enter Hidden Final Bets ===");
            Console.WriteLine("Enter each pile in table order: BOTTOM first, TOP last.\n");

            var normalCamels = Board.Camels
                .Where(c => c.Color != "white" && c.Color != "black")
                .Select(c => c.Color)
                .Distinct()
                .ToList();

            // -----------------------------
            // Winner pile
            // -----------------------------
            for (int i = 0; i < winnerPilePlayers.Count; i++)
            {
                var player = winnerPilePlayers[i];

                Console.WriteLine($"\nWinner pile bet {i + 1} of {winnerPilePlayers.Count} (BOTTOM -> TOP)");
                Console.WriteLine($"This card belongs to: {player.Name}");

                string colorInput;
                while (true)
                {
                    Console.WriteLine("Available camel colors:");
                    foreach (var color in normalCamels)
                        Console.WriteLine($"- {color}");

                    Console.Write("Enter camel color: ");
                    colorInput = Console.ReadLine()?.Trim().ToLower() ?? "";

                    if (normalCamels.Contains(colorInput))
                        break;

                    Console.WriteLine("Invalid camel color. Try again.");
                }

                AddHiddenWinnerBet(player, colorInput);
            }

            // -----------------------------
            // Loser pile
            // -----------------------------
            for (int i = 0; i < loserPilePlayers.Count; i++)
            {
                var player = loserPilePlayers[i];

                Console.WriteLine($"\nLoser pile bet {i + 1} of {loserPilePlayers.Count} (BOTTOM -> TOP)");
                Console.WriteLine($"This card belongs to: {player.Name}");

                string colorInput;
                while (true)
                {
                    Console.WriteLine("Available camel colors:");
                    foreach (var color in normalCamels)
                        Console.WriteLine($"- {color}");

                    Console.Write("Enter camel color: ");
                    colorInput = Console.ReadLine()?.Trim().ToLower() ?? "";

                    if (normalCamels.Contains(colorInput))
                        break;

                    Console.WriteLine("Invalid camel color. Try again.");
                }

                AddHiddenLoserBet(player, colorInput);
            }
        }

        // ----------------------------
        // End of race / final scoring
        // ----------------------------
        public void EndFinalScoring()
        {
            var camelOrder = Board.GetCamelOrder()
                .Where(c => c != "white" && c != "black")
                .ToList();

            Console.WriteLine("\n=== Final Race Results ===");
            Console.WriteLine("Final camel order: " + string.Join(", ", camelOrder));

            if (camelOrder.Count == 0)
                return;

            string winnerColor = camelOrder[0];
            string loserColor = camelOrder[^1];

            EnterHiddenFinalBets();

            // Official-style tiered payouts: first correct gets most
            int[] finalBetValues = { 8, 5, 3, 2, 1 };

            // Winner pile scoring
            int winnerCorrectCount = 0;
            foreach (var bet in hiddenWinnerPile)
            {
                if (bet.color == winnerColor)
                {
                    int payout = winnerCorrectCount < finalBetValues.Length
                        ? finalBetValues[winnerCorrectCount]
                        : 1;

                    bet.player.AddPoints(payout);
                    Console.WriteLine($"{bet.player.Name} correctly guessed WINNER ({bet.color}) and gets +{payout}.");
                    winnerCorrectCount++;
                }
                else
                {
                    bet.player.SubtractPoints(1);
                    Console.WriteLine($"{bet.player.Name} guessed wrong on WINNER ({bet.color}) and loses 1 point.");
                }
            }

            // Loser pile scoring
            int loserCorrectCount = 0;
            foreach (var bet in hiddenLoserPile)
            {
                if (bet.color == loserColor)
                {
                    int payout = loserCorrectCount < finalBetValues.Length
                        ? finalBetValues[loserCorrectCount]
                        : 1;

                    bet.player.AddPoints(payout);
                    Console.WriteLine($"{bet.player.Name} correctly guessed LOSER ({bet.color}) and gets +{payout}.");
                    loserCorrectCount++;
                }
                else
                {
                    bet.player.SubtractPoints(1);
                    Console.WriteLine($"{bet.player.Name} guessed wrong on LOSER ({bet.color}) and loses 1 point.");
                }
            }

            Console.WriteLine("\n=== Final Player Scores ===");
            foreach (var player in Players)
                Console.WriteLine($"{player.Name}: {player.TotalPoints} points");
        }

        private void LogGameResult()
        {
            var normalCamelOrder = Board.GetCamelOrder()
                .Where(c => c != "white" && c != "black")
                .ToList();

            string winnerCamel = normalCamelOrder.Count > 0 ? normalCamelOrder[0] : "unknown";
            string loserCamel = normalCamelOrder.Count > 0 ? normalCamelOrder[^1] : "unknown";
            string finalCamelOrder = string.Join(">", normalCamelOrder);
            string finalScores = string.Join("|", Players.Select(p => $"{p.Name}:{p.TotalPoints}"));

            string filePath = "game_results.csv";

            if (!System.IO.File.Exists(filePath))
            {
                string header = "game_id,timestamp,total_legs,winner_camel,loser_camel,winner_pile_bets,loser_pile_bets,final_camel_order,final_scores";
                System.IO.File.WriteAllText(filePath, header + Environment.NewLine);
            }

            string line =
                $"{GameId}," +
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}," +
                $"{CurrentLeg}," +
                $"{winnerCamel}," +
                $"{loserCamel}," +
                $"{TotalWinnerPileBets}," +
                $"{TotalLoserPileBets}," +
                $"\"{finalCamelOrder}\"," +
                $"\"{finalScores}\"";

            System.IO.File.AppendAllText(filePath, line + Environment.NewLine);

            Console.WriteLine("[DEBUG] Game result summary logged to game_results.csv");
        }
    }
}