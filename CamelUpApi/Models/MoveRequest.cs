public class MoveRequest
{
    public string Action { get; set; } = "";

    public string? Color { get; set; }
    public int? Roll { get; set; }

    public string? BetColor { get; set; }

    public int? TilePosition { get; set; }
    public string? TileType { get; set; }
}