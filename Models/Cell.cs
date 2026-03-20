namespace TriStrike.Models;

public class Cell
{
    public int Row { get; set; }
    public int Column { get; set; }
    public bool IsStruck { get; set; }
    public int? StrikenByPlayer { get; set; }

    public Cell(int row, int column)
    {
        Row = row;
        Column = column;
        IsStruck = false;
        StrikenByPlayer = null;
    }
}
