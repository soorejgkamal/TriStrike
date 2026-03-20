namespace TriStrike.Models;

public enum LineType
{
    Horizontal,
    Vertical,
    Diagonal
}

public class Line
{
    public LineType Type { get; set; }
    public int Length { get; set; }
    public List<Cell> Cells { get; set; }
    public bool IsCompleted { get; set; }
    public string Key { get; set; }

    public Line(LineType type, List<Cell> cells, string key)
    {
        Type = type;
        Cells = cells;
        Length = cells.Count;
        IsCompleted = false;
        Key = key;
    }
}
