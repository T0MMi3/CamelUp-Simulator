namespace CamelUpSimulator
{
    public class Camel
    {
        public string Color { get; private set; }
        public int Position { get; set; }
        public int StackHeight { get; set; } // 0 = bottom of stack

        public Camel(string color, int position = 0)
        {
            Color = color.Trim().ToLower(); 
            Position = position;
            StackHeight = 0;
        }
    }
}
