using Microsoft.AspNetCore.Mvc;
using CamelUpApi.Services;
using CamelUpApi.Models;
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

            _gameService.CreateNewGame(request.Players);

            return Ok(new
            {
                message = "Game initialized",
                players = request.Players
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
            string actionTaken = request.Action;
            string suggestion = "API move (no AI)";
            var game = _gameService.Game;
            var playerName = _gameService.GetCurrentPlayer();
            var player = game.Players.First(p => p.Name == playerName);

            if (request.Action == "roll")
            {
                if (request.Color == null || request.Roll == null)
                    return BadRequest("Color and roll required");

                var camel = game.Board.Camels.FirstOrDefault(c => c.Color == request.Color);
                if (camel == null)
                    return BadRequest("Invalid camel color");

                game.Board.MoveCamel(camel, request.Roll.Value, game);
                game.DicePyramid.UseDie(request.Color);
                game.GrantPyramidTicket(player);
            }

            _gameService.Game.Logger.LogTurn(
                _gameService.Game,
                _gameService.Game.Players[0].Name,
                suggestion,
                actionTaken
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