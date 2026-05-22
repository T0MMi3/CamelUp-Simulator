using CamelUpSimulator;

namespace CamelUpApi.Services
{
    public class GameService
    {
        public Game Game { get; private set; } = null!;
        public int CurrentPlayerIndex { get; private set; } = 0;
        public bool CanUndo { get; private set; } = false;

        private GameSnapshot? _snapshot = null;

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
            _snapshot = null;
            CanUndo = false;
        }

        public void SaveSnapshot()
        {
            _snapshot = new GameSnapshot(Game, CurrentPlayerIndex);
            CanUndo = true;
        }

        public bool Undo()
        {
            if (_snapshot == null || !CanUndo) return false;
            _snapshot.Restore(Game, out int restoredIndex);
            CurrentPlayerIndex = restoredIndex;
            _snapshot = null;
            CanUndo = false;
            return true;
        }

        public string GetCurrentPlayer()
        {
            if (Game == null || Game.Players.Count == 0)
                return "No game initialized";
            return Game.Players[CurrentPlayerIndex].Name;
        }

        public void AdvanceTurn()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Game.Players.Count;
        }
    }

    public class GameSnapshot
    {
        private readonly int _playerIndex;
        private readonly int _currentLeg;
        private readonly List<(string name, int points, List<(string color, int value)> legBets, List<(string color, bool isWinner)> finalBets)> _playerData;
        private readonly List<(string color, int position, int stackHeight)> _camelData;
        private readonly List<(string owner, int position, bool isOasis)> _tileData;
        private readonly List<string> _remainingDice;
        private readonly Dictionary<string, List<int>> _legBetDecks;

        public GameSnapshot(Game game, int playerIndex)
        {
            _playerIndex = playerIndex;
            _currentLeg = game.CurrentLeg;

            _playerData = game.Players.Select(p => (
                p.Name,
                p.TotalPoints,
                p.HeldLegBets.Select(b => (b.Color, b.Value)).ToList(),
                p.HeldFinalBets.Select(b => (b.Color, b.IsWinner)).ToList()
            )).ToList();

            _camelData = game.Board.Camels
                .Select(c => (c.Color, c.Position, c.StackHeight))
                .ToList();

            _tileData = game.Board.DesertTiles
                .Select(t => (t.OwnerName, t.Position, t.IsOasis))
                .ToList();

            _remainingDice = game.DicePyramid.GetRemainingDice().ToList();
            _legBetDecks = game.GetLegBetDeckStatus();
        }

        public void Restore(Game game, out int playerIndex)
        {
            playerIndex = _playerIndex;

            // Restore board spaces
            foreach (var space in game.Board.Spaces)
                space.Clear();
            game.Board.Camels.Clear();

            foreach (var (color, position, stackHeight) in _camelData)
            {
                var camel = new Camel(color)
                {
                    Position = position,
                    StackHeight = stackHeight
                };

                int safePos = Math.Min(position, game.Board.SpacesCount - 1);
                if (position < game.Board.SpacesCount)
                    game.Board.Spaces[position].Add(camel);

                game.Board.Camels.Add(camel);
            }

            // Restore desert tiles
            game.Board.DesertTiles.Clear();
            foreach (var (owner, position, isOasis) in _tileData)
                game.Board.DesertTiles.Add(new DesertTile(owner, position, isOasis));

            // Restore dice
            game.DicePyramid.RestoreAllDice(_remainingDice);

            // Restore players
            foreach (var (name, points, legBets, finalBets) in _playerData)
            {
                var player = game.Players.FirstOrDefault(p => p.Name == name);
                if (player == null) continue;

                player.RestorePoints(points);
                player.RestoreLegBets(legBets.Select(b => new LegBet(b.color, b.value)).ToList());
                player.RestoreFinalBets(finalBets.Select(b => new FinalRaceBet(b.color, b.isWinner)).ToList());
            }

            // Restore leg bet decks
            game.RestoreLegBetDecks(_legBetDecks);
        }
    }
}