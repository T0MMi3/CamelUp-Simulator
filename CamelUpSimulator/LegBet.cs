namespace CamelUpSimulator
{
    public class LegBet
    {
        public string Color { get; }
        public int Value { get; }
        public bool IsAvailable { get; private set; }

        public LegBet(string color, int value)
        {
            Color = color;
            Value = value;
            IsAvailable = true;
        }

        public void Claim() => IsAvailable = false;
        public void Reset() => IsAvailable = true;

        public override string ToString()
        {
            return $"{Color} ({Value} pts) {(IsAvailable ? "Available" : "Taken")}";
        }

        internal void MarkTaken() => IsAvailable = false;
    }
}
