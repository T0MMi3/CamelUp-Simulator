using CamelUpSimulator;

namespace CamelUpApi.Services
{
    public class GameService
    {
        public Game Game { get; private set; }

        public GameService()
        {
            // temporary test game
            var players = new List<string> { "Tommy" };
            Game = new Game(players);
        }
    }
}