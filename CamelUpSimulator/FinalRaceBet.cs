namespace CamelUpSimulator
{
    public class FinalRaceBet
    {
        public string Color { get; private set; }
        public bool IsWinner { get; private set; }   // <--- NEW
        public bool IsAvailable { get; private set; }
        public int Value { get; private set; }

        public FinalRaceBet(string color, bool isWinner, int value = 8)
        {
            Color = color;
            IsWinner = isWinner;
            Value = value;
            IsAvailable = true;
        }

        // Called by Player when taking this bet
        public void MarkTaken()
        {
            IsAvailable = false;
        }

        // Optional reset if a new game starts
        public void Reset()
        {
            IsAvailable = true;
        }
    }
}
