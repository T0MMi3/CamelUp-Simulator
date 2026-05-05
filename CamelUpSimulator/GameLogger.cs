using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace CamelUpSimulator
{
    public class GameLogger
    {
        private readonly string _connectionString;

        public GameLogger(string dbPath = "game_logs.db")
        {
            _connectionString = $"Data Source={dbPath}";
            Initialize();
        }

        private void Initialize()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TurnLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Player TEXT,
                    Leg INTEGER,
                    BoardState TEXT,
                    DiceRemaining TEXT,
                    Suggestion TEXT,
                    Action TEXT,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";
            cmd.ExecuteNonQuery();
        }

        public void LogTurn(Game game, string player, string suggestion, string action)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            string boardState = JsonSerializer.Serialize(
                game.Board.Spaces.Select(s => s.Select(c => c.Color))
            );

            string dice = JsonSerializer.Serialize(
                game.DicePyramid.GetRemainingDice()
            );

            cmd.CommandText = @"
                INSERT INTO TurnLogs (Player, Leg, BoardState, DiceRemaining, Suggestion, Action)
                VALUES ($player, $leg, $board, $dice, $suggestion, $action);
            ";

            cmd.Parameters.AddWithValue("$player", player);
            cmd.Parameters.AddWithValue("$leg", game.CurrentLeg);
            cmd.Parameters.AddWithValue("$board", boardState);
            cmd.Parameters.AddWithValue("$dice", dice);
            cmd.Parameters.AddWithValue("$suggestion", suggestion);
            cmd.Parameters.AddWithValue("$action", action);

            cmd.ExecuteNonQuery();
        }
    }
}