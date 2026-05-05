using CamelUpSimulator;

namespace CamelUpApi.Services
{
    public class GameService
    {
        public Game Game { get; private set; }

        public GameService()
        {
            var players = new List<string> { "Tommy" };

            var camelColors = new List<string> { "blue", "green", "red", "yellow", "purple" };
            var board = new Board(16, camelColors);

            Game = new Game(players, board);
        }
    }
}