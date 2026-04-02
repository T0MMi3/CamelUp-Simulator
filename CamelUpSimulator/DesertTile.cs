public class DesertTile
{
    public string OwnerName { get; }
    public int Position { get; }
    public bool IsOasis { get; }  // Represents Cheering (+1) now

    public DesertTile(string ownerName, int position, bool isOasis)
    {
        OwnerName = ownerName;
        Position = position;
        IsOasis = isOasis;  // true = Cheering, false = Booing
    }
}

