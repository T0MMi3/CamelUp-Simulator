using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CamelUpSimulator
{
    public class Player
    {
        public string Name { get; }
        private int points;
        public int TotalPoints => points;
        private List<FinalRaceBet> heldFinalBets = new();
        private DesertTile? placedTile;
        public List<LegBet> HeldLegBets { get; private set; } = new();
        public IReadOnlyList<FinalRaceBet> HeldFinalBets => heldFinalBets.AsReadOnly();

        private int pyramidTicketsUsedThisLeg = 0;
        public void UsePyramidTicket() => pyramidTicketsUsedThisLeg++;
        public int TotalPyramidTicketsUsedThisLeg() => pyramidTicketsUsedThisLeg;
        public void ResetPyramidTickets() => pyramidTicketsUsedThisLeg = 0;

        public Player(string name)
        {
            Name = name;
            points = 3;
        }

        public void AddPoints(int value) => points += value;
        public void SubtractPoints(int value)
        {
            points = Math.Max(0, points - value);
        }

        public void ClearLegBets()
        {
            HeldLegBets.Clear();
        }

        // ----------------------------
        // Main Turn Menu
        // ----------------------------
        // ----------------------------
        // Main Turn Menu (async with AI suggestion)
        
            //string aiSuggestion = await AiClient.GetSuggestedMoveAsync(game, Name);
            //game.CurrentAiSuggestion = aiSuggestion; // Store the AI suggestion in the game state for logging and display
            //Console.WriteLine($"\n[AI Suggestion] {aiSuggestion}\n");
        // ----------------------------
        public async Task TakeTurnAsync(Game game)
        {
            Console.WriteLine($"\n-----------------------------------");
            Console.WriteLine($"It's {Name}'s turn!");
            Console.WriteLine("-----------------------------------");

            game.CurrentAiSuggestion = "";

            game.Board.PrintBoard();
            game.ShowLegProbabilities(this);
            game.DisplayLegBetStatus();

            string suggestion = await AiClient.GetSuggestedMoveAsync(game, Name);
            game.CurrentAiSuggestion = suggestion;

            Console.WriteLine("\nAI Suggestion:");
            Console.WriteLine(suggestion);

            bool done = false;
            string actionTaken = "";

            while (!done)
            {
                Console.WriteLine("\nChoose action:");
                Console.WriteLine("1 = Leg Bet");
                Console.WriteLine("2 = Place Desert Tile");
                Console.WriteLine("3 = Pyramid Ticket (Roll)");
                Console.WriteLine("4 = Final Bet (Winner/Loser pile only)");

                Console.Write("Enter action number: ");
                string? input = Console.ReadLine()?.Trim();

                if (!int.TryParse(input, out int choice))
                {
                    Console.WriteLine("Invalid choice. Try again.");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        done = TakeLegBet(game);
                        if (done) actionTaken = "Leg Bet";
                        break;

                    case 2:
                        done = PlaceDesertTile(game);
                        if (done) actionTaken = "Desert Tile";
                        break;

                    case 3:
                        done = TakePyramidTicket(game);
                        if (done) actionTaken = "Roll";
                        break;

                    case 4:
                        done = TakeFinalBet(game);
                        if (done) actionTaken = "Final Bet";
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }
                game.Logger.LogTurn(game, Name, suggestion, actionTaken);
            }
        }



        private void LogTurnData(Game game, string aiSuggestion, string actualAction, string actionDetail)
        {
            string boardState = string.Join("|",
                game.Board.Spaces.Select(s => string.Join(",", s.Select(c => c.Color))));

            string dice = string.Join(",", game.DicePyramid.GetRemainingDice());

            string line = $"{game.GameId}," +
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}," +
                            $"{Name}," +
                            $"{game.CurrentLeg}," +
                            $"{boardState}," +
                            $"{dice}," +
                            $"{game.TotalWinnerPileBets}," +
                            $"{game.TotalLoserPileBets}," +
                            $"\"{aiSuggestion.Replace("\"", "'")}\"," +
                            $"{actualAction}," +
                            $"{actionDetail}";

            if (!File.Exists("training_log.csv"))
            {
                File.WriteAllText("training_log.csv",
                    "game_id,timestamp,player,leg,board,dice,winner_pile,loser_pile,ai,action,detail\n");
            }
        }



        // ----------------------------
        // 1️⃣ Leg Bet
        // ----------------------------
        public bool TakeLegBet(Game game, string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return false;

            color = color.Trim().ToLower();

            var betCard = game.TakeLegBetCard(color);

            if (betCard == null)
            {
                Console.WriteLine($"No leg bet card available for {color}.");
                return false;
            }

            HeldLegBets.Add(betCard);

            Console.WriteLine($"{Name} takes a leg bet card for {color} ({betCard.Value} pts).");
            return true;
        }

        public bool TakeLegBet(Game game)
        {
            Console.WriteLine("\n--- Take Leg Bet ---");
            Console.WriteLine("Enter 0 to cancel and go back.\n");

            var availableColors = game.AvailableLegBetColors();

            if (availableColors.Count == 0)
            {
                Console.WriteLine("No leg bets available!");
                return false;
            }

            Console.WriteLine("Available camel colors for betting:");
            for (int i = 0; i < availableColors.Count; i++)
            {
                Console.WriteLine($"{i + 1} = {availableColors[i]}");
            }

            Console.Write("\nChoose a number (or 0 to cancel): ");
            string? input = Console.ReadLine()?.Trim();

            if (!int.TryParse(input, out int choice))
            {
                Console.WriteLine("Invalid input. Try again.");
                return false;
            }

            if (choice == 0)
            {
                Console.WriteLine("Cancelled leg bet selection.");
                return false;
            }

            if (choice < 1 || choice > availableColors.Count)
            {
                Console.WriteLine("Invalid choice. Try again.");
                return false;
            }

            string chosenColor = availableColors[choice - 1];

            return TakeLegBet(game, chosenColor);
        }


        // ----------------------------
        // 2Place Desert Tile
        // ----------------------------
        public bool PlaceDesertTile(Game game)
        {
            Console.WriteLine("\n--- Place Desert Tile ---");
            Console.WriteLine("Enter 0 to cancel and go back.\n");

            Console.Write("Enter position (2–15) for your tile, or 0 to cancel: ");
            string input = Console.ReadLine()?.Trim() ?? "";
            if (input == "0")
                return false;

            if (!int.TryParse(input, out int pos) || pos < 2 || pos > 15)
            {
                Console.WriteLine("Invalid position. Try again.");
                return false;
            }

            int spaceIndex = pos - 1;

            // Can't place on a space with camels
            if (game.Board.Spaces[spaceIndex].Count > 0)
            {
                Console.WriteLine("That space already has camels. You can't place a tile there.");
                return false;
            }

            // Can't place on a space that already has a tile
            if (game.Board.DesertTiles.Any(t => t.Position == spaceIndex))
            {
                Console.WriteLine("That space already has a desert tile.");
                return false;
            }

            // Can't place adjacent to another desert tile
            if (game.Board.DesertTiles.Any(t => Math.Abs(t.Position - spaceIndex) == 1))
            {
                Console.WriteLine("You can't place a tile directly next to another desert tile.");
                return false;
            }

            // If player already has a tile, remove the old one first
            if (placedTile != null)
            {
                game.Board.DesertTiles.RemoveAll(t => t.OwnerName == Name);
                placedTile = null;
            }

            Console.WriteLine("\nChoose tile type:");
            Console.WriteLine("1 = Cheering Space (+1)");
            Console.WriteLine("2 = Booing Space (-1)");
            Console.Write("Enter choice (or 0 to cancel): ");
            string typeInput = Console.ReadLine()?.Trim() ?? "";

            if (typeInput == "0")
                return false;

            if (!int.TryParse(typeInput, out int typeChoice) || (typeChoice != 1 && typeChoice != 2))
            {
                Console.WriteLine("Invalid choice. Try again.");
                return false;
            }

            bool isCheering = (typeChoice == 1);
            DesertTile newTile = new DesertTile(Name, spaceIndex, isCheering);

            if (game.Board.PlaceDesertTile(newTile))
            {
                Console.WriteLine($"{Name} placed a {(isCheering ? "Cheering (+1)" : "Booing (-1)")} tile at space {pos}.");
                placedTile = newTile;
                string detail = (isCheering ? "+1" : "-1") + $" space {pos}";
                LogTurnData(game, game.CurrentAiSuggestion ?? "", "Desert Tile", detail);
                return true;
            }

            Console.WriteLine("Cannot place there (occupied or invalid).");
            return false;
        }

        //Consolidated version for API use (no console input)
        public bool PlaceDesertTile(Game game, int position, string tileType)
        {
            if (position < 2 || position > 15)
            {
                Console.WriteLine("Invalid tile position.");
                return false;
            }

            int spaceIndex = position - 1;

            if (game.Board.Spaces[spaceIndex].Count > 0)
            {
                Console.WriteLine("That space already has camels.");
                return false;
            }

            if (game.Board.DesertTiles.Any(t => t.Position == spaceIndex))
            {
                Console.WriteLine("That space already has a desert tile.");
                return false;
            }

            if (game.Board.DesertTiles.Any(t => Math.Abs(t.Position - spaceIndex) == 1))
            {
                Console.WriteLine("Cannot place next to another desert tile.");
                return false;
            }

            bool isOasis =
                tileType.ToLower() == "cheering" ||
                tileType.ToLower() == "oasis" ||
                tileType == "+1";

            bool isMirage =
                tileType.ToLower() == "booing" ||
                tileType.ToLower() == "mirage" ||
                tileType == "-1";

            if (!isOasis && !isMirage)
            {
                Console.WriteLine("Invalid tile type.");
                return false;
            }

            if (placedTile != null)
            {
                game.Board.DesertTiles.RemoveAll(t => t.OwnerName == Name);
                placedTile = null;
            }

            var tile = new DesertTile(Name, spaceIndex, isOasis);

            if (!game.Board.PlaceDesertTile(tile))
            {
                Console.WriteLine("Failed to place desert tile.");
                return false;
            }

            placedTile = tile;

            Console.WriteLine($"{Name} placed a {(isOasis ? "Cheering (+1)" : "Booing (-1)")} tile at space {position}.");
            return true;
        }



        public void ResetDesertTile()
        {
            placedTile = null;
        }


        // ----------------------------
        // 3️Pyramid Ticket (Roll)
        // ----------------------------
        public bool TakePyramidTicket(Game game)
        {
            Console.WriteLine("\n--- Pyramid Ticket (Roll) ---");
            Console.WriteLine("Enter 0 to cancel and go back.\n");

            Console.WriteLine("Available dice colors:");
            foreach (var dieColor in game.DicePyramid.GetRemainingDice())
                Console.WriteLine("- " + dieColor);

            Console.Write("Which camel die came out? (or 0 to cancel): ");
            string color = Console.ReadLine()?.Trim().ToLower() ?? "";
            if (color == "0" || color == "cancel") return false;

            Console.Write("Enter dice result (1–3): ");
            string result = Console.ReadLine()?.Trim() ?? "";
            if (result == "0") return false;

            if (!int.TryParse(result, out int roll) || roll < 1 || roll > 3)
            {
                Console.WriteLine("Invalid roll number.");
                return false;
            }

            // Check if this is a crazy camel (white or black)
            if (color == "grey")
            {
                // Let the board automatically decide based on rules
                Console.WriteLine("Grey die rolled!");
                game.Board.HandleGreyDie(game, roll);
                Console.WriteLine($"{Name} rolled the grey die — crazy camel(s) moved automatically.");
            }
            else
            {
                var camel = game.Board.Camels.FirstOrDefault(c => c.Color == color);
                if (camel == null)
                {
                    Console.WriteLine($"[WARNING] No normal camel found for color {color}.");
                    return false;
                }

                game.Board.MoveCamel(camel, roll, game);
                Console.WriteLine($"{Name} rolled {color} — it moved {roll} spaces.");
            }



            game.DicePyramid.UseDie(color);
            game.GrantPyramidTicket(this);

            Console.WriteLine($"[DEBUG] Pyramid tickets used this leg: {game.DicePyramid.PyramidTicketsUsed}/5");
            LogTurnData(game, game.CurrentAiSuggestion ?? "", "Pyramid Roll", $"{color} rolled {roll}");
            return true;
        }



        // ----------------------------
        // Final Bet
        // ----------------------------
        public bool TakeFinalBet(Game game)
        {
            Console.WriteLine("\n--- Take Final Bet ---");
            Console.WriteLine("Enter 0 to cancel and go back.\n");

            Console.Write("Bet on Winner or Loser pile? (W/L or 0 to cancel): ");
            string choice = Console.ReadLine()?.Trim().ToUpper() ?? "";

            if (choice == "0" || choice == "CANCEL")
            {
                Console.WriteLine("Cancelled final bet.");
                return false;
            }

            if (choice != "W" && choice != "L")
            {
                Console.WriteLine("Invalid input. Try again.");
                return false;
            }

            string pile = (choice == "W") ? "winner" : "loser";
            game.RecordFinalPileBet(this, pile);

            Console.WriteLine($"{Name} places a final bet on the {pile} pile.");
            LogTurnData(game, game.CurrentAiSuggestion ?? "", "Final Bet", pile);

            return true;
        }

        // Consolidated version for API use (no console input)
        public bool TakeFinalBet(Game game, string color, bool chooseWinner)
        {
            color = color.Trim().ToLower();

            if (!game.Board.Camels.Any(c => c.Color == color))
            {
                Console.WriteLine("Invalid camel color.");
                return false;
            }

            bool alreadyBetOnColor = heldFinalBets.Any(b => b.Color == color);

            if (alreadyBetOnColor)
            {
                Console.WriteLine($"{Name} already placed a final bet on {color}.");
                return false;
            }

            int value = 8;

            var bet = new FinalRaceBet(color, chooseWinner, value);

            heldFinalBets.Add(bet);

            Console.WriteLine(
                $"{Name} places a final {(chooseWinner ? "winner" : "loser")} bet on {color}."
            );

            return true;
        }



        // ----------------------------
        // Scoring
        // ----------------------------
        public void ScoreLegBets(List<string> camelOrder)
        {
            Console.WriteLine($"\n--- Scoring bets for {Name} ---");

            if (HeldLegBets.Count == 0)
            {
                Console.WriteLine($"{Name} placed no leg bets this round.");
                return;
            }

            foreach (var bet in HeldLegBets)
            {
                int pos = camelOrder.IndexOf(bet.Color);

                if (pos == 0)
                {
                    AddPoints(bet.Value);
                    Console.WriteLine($"Correct! {bet.Color} finished 1st — +{bet.Value} points.");
                }
                else if (pos == 1)
                {
                    AddPoints(1);
                    Console.WriteLine($"{bet.Color} finished 2nd — +1 consolation point.");
                }
                else
                {
                    SubtractPoints(1);
                    Console.WriteLine($"{bet.Color} finished {pos + 1} — −1 point.");
                }
            }

            Console.WriteLine($"{Name}'s total is now {TotalPoints} points.\n");
            HeldLegBets.Clear(); // Clear same list
        }
    }
}
