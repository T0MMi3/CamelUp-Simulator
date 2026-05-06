using CamelUpSimulator;

namespace CamelUpApi.Services
{
    public class GameService
    {
        public Game Game { get; private set; } = null!;
        public int CurrentPlayerIndex { get; private set; } = 0;

        public void CreateNewGame(List<string> players, List<List<string>> spaces)
        {
            var board = new Board(16, new List<string>());

            for (int pos = 0; pos < spaces.Count && pos < board.SpacesCount; pos++)
            {
                for (int height = 0; height < spaces[pos].Count; height++)
                {
                    string color = spaces[pos][height].ToLower();

                    var camel = new Camel(color)
                    {
                        Position = pos,
                        StackHeight = height
                    };

                    board.Spaces[pos].Add(camel);
                    board.Camels.Add(camel);
                }
            }

            Game = new Game(players, board);
            CurrentPlayerIndex = 0;
        }


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