namespace CamelUpApi.Models
{
    public class MoveRequest
    {
        public string Action { get; set; } = "";
        public string? Color { get; set; }
        public int? Roll { get; set; }
    }
}