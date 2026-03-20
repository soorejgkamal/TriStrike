using TriStrike.Models;

namespace TriStrike.Services;

public class GameState
{
    public const int TotalRows = 10;

    public List<List<Cell>> Board { get; private set; } = new();
    public List<Player> Players { get; private set; } = new();
    public int CurrentPlayerIndex { get; private set; } = 0;
    public List<Line> CompletedLines { get; private set; } = new();
    public HashSet<string> CompletedLineKeys { get; private set; } = new();

    public string? LastMoveMessage { get; private set; }
    public bool GameOver { get; private set; } = false;
    public Player? Winner { get; private set; }

    public event Action? OnChange;

    public GameState()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        Board = new List<List<Cell>>();
        Players = new List<Player>
        {
            new Player(1, "Player 1"),
            new Player(2, "Player 2")
        };
        CurrentPlayerIndex = 0;
        CompletedLines = new List<Line>();
        CompletedLineKeys = new HashSet<string>();
        LastMoveMessage = null;
        GameOver = false;
        Winner = null;

        InitializeBoard();
        NotifyStateChanged();
    }

    private void InitializeBoard()
    {
        Board.Clear();
        for (int row = 1; row <= TotalRows; row++)
        {
            var rowCells = new List<Cell>();
            for (int col = 1; col <= row; col++)
            {
                rowCells.Add(new Cell(row, col));
            }
            Board.Add(rowCells);
        }
    }

    public void StrikeCell(int row, int col)
    {
        if (GameOver) return;

        // row/col are 1-based
        var cell = Board[row - 1][col - 1];
        if (cell.IsStruck) return;

        cell.IsStruck = true;
        cell.StrikenByPlayer = Players[CurrentPlayerIndex].Id;

        var newLines = CheckCompletedLines(row, col);
        int pointsScored = CalculateScore(newLines);

        if (pointsScored > 0)
        {
            Players[CurrentPlayerIndex].Score += pointsScored;
            LastMoveMessage = BuildStrikeMessage(newLines, pointsScored);
        }
        else
        {
            LastMoveMessage = null;
        }

        if (IsGameOver())
        {
            GameOver = true;
            Winner = Players.OrderByDescending(p => p.Score).First();
        }
        else
        {
            SwitchTurn();
        }

        NotifyStateChanged();
    }

    public List<Line> CheckCompletedLines(int row, int col)
    {
        var newlyCompleted = new List<Line>();

        // Check horizontal line for this row
        var hLine = GetHorizontalLine(row);
        if (hLine != null && !CompletedLineKeys.Contains(hLine.Key) && hLine.Cells.All(c => c.IsStruck))
        {
            hLine.IsCompleted = true;
            CompletedLines.Add(hLine);
            CompletedLineKeys.Add(hLine.Key);
            newlyCompleted.Add(hLine);
        }

        // Check vertical line for this column
        var vLine = GetVerticalLine(col);
        if (vLine != null && !CompletedLineKeys.Contains(vLine.Key) && vLine.Cells.All(c => c.IsStruck))
        {
            vLine.IsCompleted = true;
            CompletedLines.Add(vLine);
            CompletedLineKeys.Add(vLine.Key);
            newlyCompleted.Add(vLine);
        }

        // Check main diagonal (col 1 of each row, forms left edge going down)
        // Also check the hypotenuse diagonal (rightmost cell of each row)
        var diagLine = GetDiagonalLine();
        if (diagLine != null && !CompletedLineKeys.Contains(diagLine.Key) && diagLine.Cells.All(c => c.IsStruck))
        {
            diagLine.IsCompleted = true;
            CompletedLines.Add(diagLine);
            CompletedLineKeys.Add(diagLine.Key);
            newlyCompleted.Add(diagLine);
        }

        return newlyCompleted;
    }

    /// <summary>
    /// Returns horizontal line (all cells in a row).
    /// </summary>
    public Line? GetHorizontalLine(int row)
    {
        if (row < 1 || row > TotalRows) return null;
        var cells = Board[row - 1].ToList();
        return new Line(LineType.Horizontal, cells, $"H{row}");
    }

    /// <summary>
    /// Returns vertical line (all cells with the given column index across rows that have that column).
    /// Column col exists in rows col..TotalRows.
    /// </summary>
    public Line? GetVerticalLine(int col)
    {
        if (col < 1 || col > TotalRows) return null;
        var cells = new List<Cell>();
        for (int row = col; row <= TotalRows; row++)
        {
            cells.Add(Board[row - 1][col - 1]);
        }
        return new Line(LineType.Vertical, cells, $"V{col}");
    }

    /// <summary>
    /// Returns the main diagonal: rightmost cell in each row (hypotenuse).
    /// Row 1 → col 1, Row 2 → col 2, ..., Row 10 → col 10.
    /// This is the top-left to bottom-right hypotenuse.
    /// </summary>
    public Line? GetDiagonalLine()
    {
        var cells = new List<Cell>();
        for (int row = 1; row <= TotalRows; row++)
        {
            cells.Add(Board[row - 1][row - 1]);
        }
        return new Line(LineType.Diagonal, cells, "D1");
    }

    public int CalculateScore(List<Line> lines)
    {
        return lines.Sum(l => l.Length);
    }

    public void SwitchTurn()
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
    }

    public bool IsGameOver()
    {
        return Board.SelectMany(row => row).All(cell => cell.IsStruck);
    }

    public Player CurrentPlayer => Players[CurrentPlayerIndex];

    private string BuildStrikeMessage(List<Line> newLines, int points)
    {
        if (newLines.Count == 0) return string.Empty;

        // Special case: bottom row + first column (vertical col 1) + diagonal all at once
        bool hasBottom = newLines.Any(l => l.Type == LineType.Horizontal && l.Cells.First().Row == TotalRows);
        bool hasFirstCol = newLines.Any(l => l.Type == LineType.Vertical && l.Cells.First().Column == 1);
        bool hasDiag = newLines.Any(l => l.Type == LineType.Diagonal);

        if (hasBottom && hasFirstCol && hasDiag)
        {
            return $"⚡ TRI-STRIKE! Combined score: {points} points!";
        }

        string strikeLabel = newLines.Count switch
        {
            1 => "Strike",
            2 => "Double Strike",
            3 => "Triple Strike",
            _ => $"{newLines.Count}x Strike"
        };

        var lineDescriptions = newLines.Select(l => $"{l.Type} (length {l.Length})");
        return $"🎯 {strikeLabel}! Completed: {string.Join(", ", lineDescriptions)}. +{points} points!";
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
