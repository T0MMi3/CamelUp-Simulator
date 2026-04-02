using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CamelUpSimulator
{
    public static class AiClient
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> GetSuggestedMoveAsync(Game game, string currentPlayer)
        {
            // Construct a simple, serializable snapshot of the board state
            var payload = new
            {
                spaces = game.Board.Spaces.ConvertAll(
                    stack => stack.ConvertAll(c => c.Color)
                ),
                dice_remaining = game.DicePyramid.GetRemainingDice(),
                winner_pile_bets = game.TotalWinnerPileBets,
                loser_pile_bets = game.TotalLoserPileBets,
                turn_player = currentPlayer
            };

            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("http://127.0.0.1:8000/predict", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"[AI ERROR] {ex.Message}";
            }
        }
    }
}
