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
                camels = game.Board.Camels.Select(c => new
                {
                    c.Color,
                    c.Position,
                    c.StackHeight
                }),
                currentLeg = game.CurrentLeg,
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

                    if (request.Color == null || request.Roll == null)
                        return BadRequest("Color and roll required");

                    var camel = game.Board.Camels
                        .FirstOrDefault(c => c.Color == request.Color);

                    if (camel == null)
                        return BadRequest("Invalid camel color");

                    game.Board.MoveCamel(camel, request.Roll.Value, game);
                    game.DicePyramid.UseDie(request.Color);
                    game.GrantPyramidTicket(player);

                    break;

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

                    // later:
                    // winner bet logic

                    break;

                case "loserbet":

                    // later:
                    // loser bet logic

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