using CamelUpSimulator;

namespace CamelUpApi.Services
{
    public class GameService
    {
        public Game Game { get; private set; }

        public void CreateNewGame(List<string> players)
        {
            var camelColors = new List<string> { "blue", "green", "red", "yellow", "purple" };
            var board = new Board(16, camelColors);

            Game = new Game(players, board);
        }

        public int CurrentPlayerIndex { get; private set; } = 0;

        public string GetCurrentPlayer()
        {
            return Game.Players[CurrentPlayerIndex].Name;
        }

        public void AdvanceTurn()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Game.Players.Count;
        }
    }
}