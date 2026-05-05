using Microsoft.AspNetCore.Mvc;
using CamelUpApi.Services;

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
    }
}