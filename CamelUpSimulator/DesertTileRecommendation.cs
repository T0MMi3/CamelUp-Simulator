namespace CamelUpSimulator
{
    public class DesertTileRecommendation
    {
        public int Position { get; set; }              // 0-based
        public bool IsCheering { get; set; }           // true = +1, false = -1
        public double PortfolioEVBefore { get; set; }
        public double PortfolioEVAfter { get; set; }
        public double TileHitProbability { get; set; } // 0 to 1
        public double TotalScore { get; set; }
    }
}