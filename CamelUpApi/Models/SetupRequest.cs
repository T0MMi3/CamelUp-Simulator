namespace CamelUpApi.Models
{
    public class SetupRequest
    {
        public List<string> Players { get; set; } = new();
        public List<List<string>> Spaces { get; set; } = new();
    }
}