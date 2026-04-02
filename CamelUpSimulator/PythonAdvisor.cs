using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CamelUpSimulator
{
    public class PythonAdvisor
    {
        private readonly HttpClient _client;

        public PythonAdvisor()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1:8000/")
            };
        }

        // Sends board state and returns suggestion string
        public async Task<string> GetSuggestionAsync(Board board, List<string> diceRemaining, string turnPlayer)
        {
            try
            {
                // Build payload
                var payload = new
                {
                    spaces = ConvertBoardToList(board),
                    dice_remaining = diceRemaining,
                    turn_player = turnPlayer
                };

                var response = await _client.PostAsJsonAsync("predict", payload);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<PredictionResponse>();
                return result?.suggestion ?? "No suggestion received";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PythonAdvisor Error] {ex.Message}");
                return "Error fetching suggestion";
            }
        }

        // Converts your Board object to nested list of camel colors
        private List<List<string>> ConvertBoardToList(Board board)
        {
            var list = new List<List<string>>();

            foreach (var space in board.Spaces)
            {
                var spaceList = new List<string>();
                foreach (var camel in space)
                {
                    spaceList.Add(camel.Color); // Assuming Camel has a Color property
                }
                list.Add(spaceList);
            }

            return list;
        }

        // Matches Python API response
        private class PredictionResponse
        {
            public string? suggestion { get; set; }
        }
    }
}
