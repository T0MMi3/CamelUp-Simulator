namespace CamelUpSimulator
{
    public class Camel
    {
        public string Color { get; private set; }
        public int Position { get; set; }
        public int Height { get; set; }
        public bool MovesForward { get; set; }

        public Camel(string color, int position = 0, int height = 0, bool movesForward = true)
        {
            Color = color;
            Position = position;
            Height = height;
            MovesForward = movesForward;
        }

        public void Move(int steps)
        {
            int dir = MovesForward ? 1 : -1;
            Position += dir * steps;
        }

        public override string ToString() =>
            $"{Color} (Pos: {Position}, Height: {Height}, Dir: {(MovesForward ? "F" : "B")})";
    }
}