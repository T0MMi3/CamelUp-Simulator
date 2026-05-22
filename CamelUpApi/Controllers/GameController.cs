using Microsoft.AspNetCore.Mvc;
using CamelUpApi.Services;
using CamelUpApi.Models;
using CamelUpSimulator;

namespace CamelUpApi.Controllers
{
    [ApiController]
    [Route("game")]
    public class GameController : ControllerBase
    {
        private readonly GameService _gameService;

        public GameController(GameService gameService)
        {
            _gameService = gameService;
        }

        // POST /game/setup
        // Initialize a new game with player names and starting board state.
        // Spaces is a 16-element array; each element is an ordered list of camel colors
        // (bottom → top) at that board position (0-indexed).
        [HttpPost("setup")]
        public IActionResult SetupGame([FromBody] SetupRequest request)
        {
            if (request.Players == null || request.Players.Count == 0)
                return BadRequest("At least one player required.");

            if (request.Spaces == null || request.Spaces.Count != 16)
                return BadRequest("Spaces must contain exactly 16 board spaces.");

            _gameService.CreateNewGame(request.Players, request.Spaces);

            return Ok(new
            {
                message = "Game initialized",
                players = request.Players,
                spaces = request.Spaces
            });
        }

        // GET /game/state
        // Returns the full current game state: camel positions, player holdings,
        // desert tiles, remaining dice, and leg-bet deck sizes.
        [HttpGet("state")]
        public IActionResult GetState()
        {
            if (_gameService.Game == null)
                return BadRequest("No game initialized. Call POST /game/setup first.");

            var game = _gameService.Game;

            var state = new
            {
                currentPlayer = _gameService.GetCurrentPlayer(),
                currentLeg = game.CurrentLeg,
                isRaceFinished = game.IsRaceFinished(),
                canUndo = _gameService.CanUndo,

                players = game.Players.Select(p => new
                {
                    name = p.Name,
                    points = p.TotalPoints,
                    legBets = p.HeldLegBets.Select(b => new { color = b.Color, value = b.Value }),
                    // Only reveal your own final bets — others show count only
                    finalBets = p.Name == _gameService.GetCurrentPlayer()
                        ? (IEnumerable<object>)p.HeldFinalBets.Select(b => (object)new
                            {
                                color = b.Color,
                                isWinner = b.IsWinner,
                                value = b.Value
                            })
                        : Enumerable.Empty<object>(),
                    winnerBetCount = p.HeldFinalBets.Count(b => b.IsWinner),
                    loserBetCount = p.HeldFinalBets.Count(b => !b.IsWinner),
                }),

                camels = game.Board.Camels.Select(c => new
                {
                    color = c.Color,
                    position = c.Position,
                    stackHeight = c.StackHeight
                }),

                desertTiles = game.Board.DesertTiles.Select(t => new
                {
                    owner = t.OwnerName,
                    position = t.Position,
                    type = t.IsOasis ? "cheering" : "booing"
                }),

                legBetDecks = game.GetLegBetDeckStatus(),
                diceRemaining = game.DicePyramid.GetRemainingDice()
            };

            return Ok(state);
        }

        // GET /game/ev
        // Runs Monte Carlo simulation (default 5000 runs) and returns expected values
        // for every available action for the current player:
        //   - Leg bets per color (EV of taking that bet card)
        //   - Roll EV (guaranteed +1 coin from pyramid ticket)
        //   - Desert tile placements ranked by EV gain
        //   - Best overall action recommendation
        [HttpGet("ev")]
        public IActionResult GetExpectedValues([FromQuery] int simulations = 5000)
        {
            if (_gameService.Game == null)
                return BadRequest("No game initialized. Call POST /game/setup first.");

            if (simulations < 100 || simulations > 50000)
                return BadRequest("simulations must be between 100 and 50000.");

            var game = _gameService.Game;
            var playerName = _gameService.GetCurrentPlayer();
            var player = game.Players.FirstOrDefault(p => p.Name == playerName);

            if (player == null)
                return BadRequest("Current player not found.");

            // ── Single Monte Carlo run shared across all calculations ─────────────────
            // IMPROVEMENT 3: Run simulation once and reuse instead of running separately
            // for leg bets and desert tiles (previously ran twice per EV call)
            // Run Monte Carlo for leg probabilities
            var probs = ProbabilityEngine.CalculateLegProbabilities(game, simulations);

            // Run separate full race simulation for final bet EVs
            var raceProbs = ProbabilityEngine.CalculateRaceProbabilities(game, Math.Min(simulations, 2000));

            // ── Leg bet EVs ───────────────────────────────────────────────────────────
            var legBetEVs = game.GetLegBetExpectedValues(probs);
            var legBetGains = game.GetPlayerLegBetEVGain(player, probs);

            var legBetOptions = legBetEVs
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => new
                {
                    color = kvp.Key,
                    cardEV = Math.Round(kvp.Value, 3),
                    portfolioGain = legBetGains.ContainsKey(kvp.Key)
                        ? Math.Round(legBetGains[kvp.Key], 3)
                        : 0,
                    winPct = Math.Round(probs.FirstPlaceOdds[kvp.Key], 1),
                    secondPct = Math.Round(probs.SecondPlaceOdds[kvp.Key], 1)
                })
                .ToList();

            // ── Roll EV ───────────────────────────────────────────────────────────────
            double rollEV = 1.0;

            // ── Desert tile recommendations ───────────────────────────────────────────
            // Pass the already-computed baseline probs so EvaluateDesertTilePlacements
            // doesn't re-run the simulation for the baseline (IMPROVEMENT 3 continued)
            var tileRecs = ProbabilityEngine.EvaluateDesertTilePlacements(game, player, probs, Math.Min(simulations, 2000));
            var topTileRecs = tileRecs
                .Take(5)
                .Select(r => new
                {
                    position = r.Position,
                    positionLabel = r.Position + 1,
                    type = r.IsCheering ? "cheering" : "booing",
                    evGain = Math.Round(r.TotalScore, 3),
                    hitProbability = Math.Round(r.TileHitProbability, 3),
                    portfolioEVBefore = Math.Round(r.PortfolioEVBefore, 3),
                    portfolioEVAfter = Math.Round(r.PortfolioEVAfter, 3)
                })
                .ToList();

            // ── IMPROVEMENT 1: Final bet EVs (winner and loser pile) ──────────────────
            // Payout tiers: first correct bet pays most, later correct bets pay less
            int[] finalBetPayouts = { 8, 5, 3, 2, 1 };
            var normalColors = game.Board.Camels
                .Where(c => c.Color != "white" && c.Color != "black")
                .Select(c => c.Color)
                .Distinct()
                .ToList();

            // Dynamic threshold based on game progress
            int diceLeft = game.DicePyramid.GetRemainingDice().Count;
            double gameProgress = Math.Min(1.0, (game.CurrentLeg - 1 + (1 - diceLeft / 6.0)) / 3.0);
            double baseFinalBetThreshold = 0.60 - (gameProgress * 0.25);

            // Per-color threshold — lower it if this camel is a heavy outlier vs the field
            double avgWinProb = 1.0 / normalColors.Count;
            double avgLoseProb = 1.0 / normalColors.Count;

            // Colors the current player has already used (can't bet same color twice)
            var usedColors = player.HeldFinalBets.Select(b => b.Color).ToHashSet();

            var finalBetOptions = normalColors
                .Where(color => !usedColors.Contains(color))
                .Select(color =>
                {
                    // How many correct winner bets already placed across all players?
                    // Payout tier is determined by total cards in pile — this is public info
                    int winnerBetsAlready = game.Players
                        .Sum(p => p.HeldFinalBets.Count(b => b.IsWinner));
                    int loserBetsAlready = game.Players
                        .Sum(p => p.HeldFinalBets.Count(b => !b.IsWinner));

                    int winnerPayout = winnerBetsAlready < finalBetPayouts.Length
                        ? finalBetPayouts[winnerBetsAlready] : 1;
                    int loserPayout = loserBetsAlready < finalBetPayouts.Length
                        ? finalBetPayouts[loserBetsAlready] : 1;

                    // EV = P(correct) * payout + P(wrong) * (-1)
                    // Count existing bets for penalty calculation
                    int existingWinnerBets = player.HeldFinalBets.Count(b => b.IsWinner);
                    int existingLoserBets = player.HeldFinalBets.Count(b => !b.IsWinner);

                    // Use full race probabilities for final bet EVs
                    double winProb = raceProbs.RaceWinOdds.ContainsKey(color)
                        ? raceProbs.RaceWinOdds[color] / 100.0
                        : 0;
                    double loseProb = raceProbs.RaceLastOdds.ContainsKey(color)
                        ? raceProbs.RaceLastOdds[color] / 100.0
                        : 0;

                    // Outlier factor — lower threshold for camels far from average
                    double winOutlierFactor = avgWinProb > 0 ? winProb / avgWinProb : 1.0;
                    double loseOutlierFactor = avgLoseProb > 0 ? loseProb / avgLoseProb : 1.0;

                    double winThreshold = Math.Max(0.35, baseFinalBetThreshold - Math.Max(0, (1 - winOutlierFactor) * 0.15));
                    double loseThreshold = Math.Max(0.35, baseFinalBetThreshold - Math.Max(0, (loseOutlierFactor - 1) * 0.15));

                    double winnerEV = winProb >= winThreshold
                        ? (winProb * winnerPayout + (1 - winProb) * (-1)) - existingWinnerBets
                        : double.NegativeInfinity;

                    double loserEV = loseProb >= loseThreshold
                        ? (loseProb * loserPayout + (1 - loseProb) * (-1)) - existingLoserBets
                        : double.NegativeInfinity;

                    return new
                    {
                        color,
                        winnerEV = winnerEV == double.NegativeInfinity ? (double?)null : Math.Round(winnerEV, 3),
                        loserEV = loserEV == double.NegativeInfinity ? (double?)null : Math.Round(loserEV, 3),
                        winnerPayout,
                        loserPayout,
                        winPct = Math.Round(probs.FirstPlaceOdds[color], 1),
                        lastPct = Math.Round(loseProb * 100, 1),
                    };
                })
                .OrderByDescending(x => Math.Max(x.winnerEV ?? double.NegativeInfinity, x.loserEV ?? double.NegativeInfinity))
                .ToList();

            // ── IMPROVEMENT 2: Portfolio-aware recommendation ─────────────────────────
            // Compare all action EVs including final bets, pick the single best action
            double bestLegBetGain = legBetGains.Count > 0
                ? legBetGains.Values.Max()
                : double.NegativeInfinity;

            double bestTileGain = tileRecs.Count > 0
                ? tileRecs[0].TotalScore
                : double.NegativeInfinity;

            double bestWinnerEV = finalBetOptions.Count > 0
                ? finalBetOptions.Max(x => x.winnerEV ?? double.NegativeInfinity)
                : double.NegativeInfinity;

            double bestLoserEV = finalBetOptions.Count > 0
                ? finalBetOptions.Max(x => x.loserEV ?? double.NegativeInfinity)
                : double.NegativeInfinity;

            double bestFinalBetEV = Math.Max(bestWinnerEV, bestLoserEV);

            // Find the single highest EV action across everything
            double[] allEVs = { rollEV, bestLegBetGain, bestTileGain, bestFinalBetEV };
            double maxEV = allEVs.Max();

            string recommendation;
            string recommendedAction;

            if (maxEV == rollEV && rollEV >= bestLegBetGain && rollEV >= bestTileGain && rollEV >= bestFinalBetEV)
            {
                recommendation = $"Roll the pyramid (+1 guaranteed; best leg bet gain: {bestLegBetGain:0.00}, best final bet EV: {bestFinalBetEV:0.00})";
                recommendedAction = "roll";
            }
            else if (bestLegBetGain >= bestTileGain && bestLegBetGain >= bestFinalBetEV && bestLegBetGain >= rollEV)
            {
                var best = legBetGains.OrderByDescending(kvp => kvp.Value).First();
                recommendation = $"Take {best.Key} leg bet (EV gain: {best.Value:0.00})";
                recommendedAction = $"legbet:{best.Key}";
            }
            else if (bestFinalBetEV >= bestTileGain && bestFinalBetEV >= rollEV && bestFinalBetEV >= bestLegBetGain)
            {
                if (bestWinnerEV >= bestLoserEV)
                {
                    var best = finalBetOptions.OrderByDescending(x => x.winnerEV).First();
                    recommendation = $"Place {best.color} winner bet (EV: {best.winnerEV:0.00}, pays {best.winnerPayout} if correct)";
                    recommendedAction = $"winnerbet:{best.color}";
                }
                else
                {
                    var best = finalBetOptions.OrderByDescending(x => x.loserEV).First();
                    recommendation = $"Place {best.color} loser bet (EV: {best.loserEV:0.00}, pays {best.loserPayout} if correct)";
                    recommendedAction = $"loserbet:{best.color}";
                }
            }
            else
            {
                var bestTile = tileRecs[0];
                recommendation = $"Place {(bestTile.IsCheering ? "cheering" : "booing")} tile at space {bestTile.Position + 1} (EV gain: {bestTile.TotalScore:0.00})";
                recommendedAction = $"deserttile:{bestTile.Position}:{(bestTile.IsCheering ? "cheering" : "booing")}";
            }

            return Ok(new
            {
                currentPlayer = playerName,
                simulationsRun = probs.SimulationsRun,
                camelProbabilities = probs.FirstPlaceOdds.Keys
                    .OrderByDescending(c => probs.FirstPlaceOdds[c])
                    .Select(c => new
                    {
                        color = c,
                        winPct = Math.Round(probs.FirstPlaceOdds[c], 1),
                        secondPct = Math.Round(probs.SecondPlaceOdds[c], 1)
                    }),
                legBetOptions,
                rollEV,
                desertTileOptions = topTileRecs,
                finalBetOptions,
                raceWinOdds = raceProbs.RaceWinOdds,
                raceLastOdds = raceProbs.RaceLastOdds,
                recommendation,
                recommendedAction
            });
        }

        // POST /game/move
        // Apply a move for the current player. Actions:
        //   roll       – color (string), roll (int 1–3)
        //   legbet     – betColor (string)
        //   deserttile – tilePosition (int 0-indexed), tileType ("cheering" | "booing")
        //   winnerbet  – betColor (string)
        //   loserbet   – betColor (string)
        [HttpPost("move")]
        public IActionResult MakeMove([FromBody] MoveRequest request)
        {
            if (_gameService.Game == null)
                return BadRequest("No game initialized.");
            
            List<Game.LegBetResult>? legSummary = null;
            string[]? legCamelOrder = null;
            string logMessage = "";
             _gameService.SaveSnapshot();
            var game = _gameService.Game;
            var playerName = _gameService.GetCurrentPlayer();
            var player = game.Players.FirstOrDefault(p => p.Name == playerName);

            if (player == null)
                return BadRequest("Current player not found.");

            switch (request.Action.ToLower())
            {
                case "roll":
                {
                    if (request.Color == null || request.Roll == null)
                        return BadRequest("Color and roll are required for action 'roll'.");

                    string color = request.Color.Trim().ToLower();
                    int roll = request.Roll.Value;

                    if (roll < 1 || roll > 3)
                        return BadRequest("Roll must be between 1 and 3.");

                    if (!game.DicePyramid.IsDieAvailable(color))
                        return BadRequest($"Die '{color}' has already been used this leg.");

                    if (color == "grey")
                    {
                        string crazyCamelColor = request.CrazyCamel?.Trim().ToLower() ?? "white";
                        var crazyCamel = game.Board.Camels.FirstOrDefault(c => c.Color == crazyCamelColor);
                        int oldPos = crazyCamel?.Position ?? 0;
                        game.Board.HandleGreyDie(game, roll, crazyCamelColor);
                        game.DicePyramid.UseDie("grey");
                        game.GrantPyramidTicket(player);
                        int newPos = crazyCamel?.Position ?? 0;
                        logMessage = $"rolled grey → {crazyCamelColor} moved {roll} back (space {oldPos + 1} → {newPos + 1})";
                    }
                    else
                    {
                        var camel = game.Board.Camels.FirstOrDefault(c => c.Color.ToLower() == color);
                        if (camel == null)
                            return BadRequest($"No camel with color '{color}'.");

                        int oldPos = camel.Position;
                        game.Board.MoveCamel(camel, roll, game);
                        game.DicePyramid.UseDie(color);
                        game.GrantPyramidTicket(player);
                        int newPos = camel.Position;
                        logMessage = $"rolled {color} {roll} (space {oldPos + 1} → {newPos + 1})";
                    }

                    if (game.DicePyramid.PyramidTicketsUsed >= 5)
                    {
                        legCamelOrder = game.Board.GetCamelOrder()
                            .Where(c => c != "white" && c != "black")
                            .ToArray();
                        legSummary = game.GetLegBetResults();
                        game.EndLegScoring();
                        logMessage += " [LEG ENDED]";
                    }

                    break;
                }

                case "legbet":
                {
                    if (request.BetColor == null)
                        return BadRequest("BetColor is required for action 'legbet'.");

                    var preview = game.TakeLegBetCardPreview(request.BetColor);
                    bool success = player.TakeLegBet(game, request.BetColor);

                    if (!success)
                        return BadRequest($"Leg bet on '{request.BetColor}' is unavailable or invalid.");

                    logMessage = $"took {request.BetColor} leg bet ({preview?.Value ?? 0}pts card)";
                    break;
                }

                case "deserttile":
                {
                    if (request.TilePosition == null || request.TileType == null)
                        return BadRequest("TilePosition and TileType are required.");

                    bool placed = player.PlaceDesertTile(
                        game,
                        request.TilePosition.Value,
                        request.TileType
                    );

                    if (!placed)
                        return BadRequest("Invalid desert tile placement (occupied, adjacent tile, or invalid position).");

                    logMessage = $"placed {request.TileType} tile at space {request.TilePosition.Value}";
                    break;
                }

                case "winnerbet":
                {
                    if (request.BetColor == null)
                        return BadRequest("BetColor is required for action 'winnerbet'.");

                    bool success = player.TakeFinalBet(game, request.BetColor, true);

                    if (!success)
                        return BadRequest($"Winner bet on '{request.BetColor}' is invalid.");

                    logMessage = $"placed a card in the winner pile";
                    break;
                }

                case "loserbet":
                {
                    if (request.BetColor == null)
                        return BadRequest("BetColor is required for action 'loserbet'.");

                    bool success = player.TakeFinalBet(game, request.BetColor, false);

                    if (!success)
                        return BadRequest($"Loser bet on '{request.BetColor}' is invalid.");

                    logMessage = $"placed a card in the loser pile";
                    break;
                }

                default:
                    return BadRequest($"Unknown action '{request.Action}'. Valid: roll, legbet, deserttile, winnerbet, loserbet.");
            }

            game.Logger.LogTurn(game, player.Name, "API move", request.Action);
            _gameService.AdvanceTurn();

            return Ok(new
            {
                message = "Move applied",
                action = request.Action,
                player = playerName,
                logMessage,  // ← add this
                currentPlayer = _gameService.GetCurrentPlayer(),
                isRaceFinished = game.IsRaceFinished(),
                diceRemaining = game.DicePyramid.GetRemainingDice(),
                camels = game.Board.Camels.Select(c => new
                {
                    color = c.Color,
                    position = c.Position,
                    stackHeight = c.StackHeight
                }),
                legEnded = legSummary != null,
                legCamelOrder,
                legSummary
            });
        }

        [HttpPost("undo")]
        public IActionResult UndoMove()
        {
            if (_gameService.Game == null)
                return BadRequest("No game initialized.");

            if (!_gameService.CanUndo)
                return BadRequest("Nothing to undo.");

            bool success = _gameService.Undo();

            if (!success)
                return BadRequest("Undo failed.");

            return Ok(new
            {
                message = "Last move undone",
                currentPlayer = _gameService.GetCurrentPlayer(),
                camels = _gameService.Game.Board.Camels.Select(c => new
                {
                    color = c.Color,
                    position = c.Position,
                    stackHeight = c.StackHeight
                }),
                diceRemaining = _gameService.Game.DicePyramid.GetRemainingDice()
            });
        }

        // POST /game/finalize
        // Triggers final scoring. Pass the actual finishing camel order (1st to last).
        [HttpPost("finalize")]
        public IActionResult FinalizeGame()
        {
            if (_gameService.Game == null)
                return BadRequest("No game initialized.");

            var game = _gameService.Game;

            if (!game.IsRaceFinished())
                return BadRequest("Race is not finished yet.");

            var camelOrder = game.Board.GetCamelOrder()
                .Where(c => c != "white" && c != "black")
                .ToList();

            if (camelOrder.Count == 0)
                return BadRequest("Could not determine camel order.");

            string winnerColor = camelOrder[0];
            string loserColor = camelOrder[^1];

            int[] payouts = { 8, 5, 3, 2, 1 };

            var breakdown = new List<object>();

            // Score winner pile — need to get bets in order placed
            var winnerBets = game.Players
                .SelectMany(p => p.HeldFinalBets
                    .Where(b => b.IsWinner)
                    .Select(b => new { player = p, bet = b }))
                .ToList();

            int winnerCorrect = 0;
            foreach (var entry in winnerBets)
            {
                bool correct = entry.bet.Color == winnerColor;
                int payout = correct
                    ? (winnerCorrect < payouts.Length ? payouts[winnerCorrect] : 1)
                    : -1;

                if (correct) winnerCorrect++;
                if (correct) entry.player.AddPoints(payout);
                else entry.player.SubtractPoints(1);

                breakdown.Add(new
                {
                    player = entry.player.Name,
                    pile = "winner",
                    color = entry.bet.Color,
                    correct,
                    payout
                });
            }

            // Score loser pile
            var loserBets = game.Players
                .SelectMany(p => p.HeldFinalBets
                    .Where(b => !b.IsWinner)
                    .Select(b => new { player = p, bet = b }))
                .ToList();

            int loserCorrect = 0;
            foreach (var entry in loserBets)
            {
                bool correct = entry.bet.Color == loserColor;
                int payout = correct
                    ? (loserCorrect < payouts.Length ? payouts[loserCorrect] : 1)
                    : -1;

                if (correct) loserCorrect++;
                if (correct) entry.player.AddPoints(payout);
                else entry.player.SubtractPoints(1);

                breakdown.Add(new
                {
                    player = entry.player.Name,
                    pile = "loser",
                    color = entry.bet.Color,
                    correct,
                    payout
                });
            }

            var finalStandings = game.Players
                .OrderByDescending(p => p.TotalPoints)
                .Select((p, i) => new
                {
                    rank = i + 1,
                    name = p.Name,
                    points = p.TotalPoints
                })
                .ToList();

            return Ok(new
            {
                winnerCamel = winnerColor,
                loserCamel = loserColor,
                finalCamelOrder = camelOrder,
                betBreakdown = breakdown,
                finalStandings
            });
        }
    }
}