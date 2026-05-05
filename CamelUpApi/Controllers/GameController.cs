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
            var player = game.Players[0]; // temp: single player

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

            return Ok(new
            {
                message = "Move applied",
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