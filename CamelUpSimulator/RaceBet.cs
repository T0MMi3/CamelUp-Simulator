public class RaceBet
{
    public string CamelColor { get; set; }
    public int Payout { get; set; }          // End-of-race payout
    public bool Claimed { get; set; } = false;

    public RaceBet(string camelColor, int payout)
    {
        CamelColor = camelColor;
        Payout = payout;
    }
}
