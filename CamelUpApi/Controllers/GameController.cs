using Microsoft.AspNetCore.Mvc;
using CamelUpApi.Services;
using CamelUpApi.Models;

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

        [HttpPost("setup")]
        public IActionResult SetupGame([FromBody] SetupRequest request)
        {
            if (request.Players == null || request.Players.Count == 0)
                return BadRequest("At least one player required");

            if (request.Spaces == null || request.Spaces.Count != 16)
                return BadRequest("Spaces must contain exactly 16 board spaces");

            _gameService.CreateNewGame(request.Players, request.Spaces);

            return Ok(new
            {
                message = "Game initialized",
                players = request.Players,
                spaces = request.Spaces
            });
        }

        [HttpGet("state")]
        public IActionResult GetState()
        {
            var game = _gameService.Game;

            var state = new
            {
                currentPlayer = _gameService.GetCurrentPlayer(),

                currentLeg = game.CurrentLeg,

                players = game.Players.Select(p => new
                {
                    name = p.Name,
                    points = p.TotalPoints,

                    legBets = p.LegBets.Select(b => new
                    {
                        color = b.Color,
                        value = b.Value
                    }),

                    finalBets = p.HeldFinalBets.Select(b => new
                    {
                        color = b.Color,
                        isWinner = b.IsWinner,
                        value = b.Value
                    })
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

                diceRemaining = game.DicePyramid.GetRemainingDice()
            };

            return Ok(state);
        }

        [HttpPost("move")]
        public IActionResult MakeMove([FromBody] MoveRequest request)
        {
            var game = _gameService.Game;

            var playerName = _gameService.GetCurrentPlayer();
            var player = game.Players.First(p => p.Name == playerName);

            switch (request.Action.ToLower())
            {
                case "roll":
                {
                    if (request.Color == null || request.Roll == null)
                        return BadRequest("Color and roll required");

                    string color = request.Color.Trim().ToLower();
                    int roll = request.Roll.Value;

                    if (roll < 1 || roll > 3)
                        return BadRequest("Roll must be between 1 and 3.");

                    if (!game.DicePyramid.IsDieAvailable(color))
                        return BadRequest("That die has already been used this leg.");

                    if (color == "grey")
                    {
                        game.Board.HandleGreyDie(game, roll);

                        game.DicePyramid.UseDie("grey");
                        game.GrantPyramidTicket(player);
                    }
                    else
                    {
                        var camel = game.Board.Camels
                            .FirstOrDefault(c => c.Color.ToLower() == color);

                        if (camel == null)
                            return BadRequest("Invalid camel color");

                        game.Board.MoveCamel(camel, roll, game);
                        game.DicePyramid.UseDie(color);
                        game.GrantPyramidTicket(player);
                    }

                    if (game.DicePyramid.PyramidTicketsUsed >= 5)
                    {
                        game.EndLegScoring();
                    }

                    break;
                }

                case "legbet":
                    if (request.BetColor == null)
                        return BadRequest("BetColor required");

                    bool success = player.TakeLegBet(game, request.BetColor);

                    if (!success)
                        return BadRequest("Invalid or unavailable leg bet");

                    break;

                case "deserttile":
                    if (request.TilePosition == null || request.TileType == null)
                        return BadRequest("TilePosition and TileType required");

                    bool tilePlaced = player.PlaceDesertTile(
                        game,
                        request.TilePosition.Value,
                        request.TileType
                    );

                    if (!tilePlaced)
                        return BadRequest("Invalid desert tile placement");

                    break;

                case "winnerbet":
                    if (request.BetColor == null)
                        return BadRequest("BetColor required");

                    bool winnerSuccess =
                        player.TakeFinalBet(game, request.BetColor, true);

                    if (!winnerSuccess)
                        return BadRequest("Invalid winner bet");

                    break;


                case "loserbet":
                    if (request.BetColor == null)
                        return BadRequest("BetColor required");

                    bool loserSuccess =
                        player.TakeFinalBet(game, request.BetColor, false);

                    if (!loserSuccess)
                        return BadRequest("Invalid loser bet");

                    break;

                default:
                    return BadRequest("Invalid action");
            }

            game.Logger.LogTurn(
                game,
                player.Name,
                "API move (no AI)",
                request.Action
            );

            _gameService.AdvanceTurn();

            return Ok(new
            {
                message = "Move applied",
                currentPlayer = _gameService.GetCurrentPlayer(),

                state = new
                {
                    camels = game.Board.Camels.Select(c => new
                    {
                        c.Color,
                        c.Position,
                        c.StackHeight
                    }),
                    diceRemaining = game.DicePyramid.GetRemainingDice()
                }
            });
        }
    }
    
}