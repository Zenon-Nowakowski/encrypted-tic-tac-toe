namespace TicTacToe.Common;

public sealed class GameBoard
{
    private readonly char[] _cells = [' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' '];

    public static readonly int[][] WinLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // cols
        [0, 4, 8], [2, 4, 6],             // diagonals
    ];

    public char this[int index]
    {
        get
        {
            return _cells[index];
        }
    }

    public int[] FreeCells
    {
        get
        {
            List<int> free = new List<int>();
            for (int i = 0; i < 9; i++)
            {
                if (_cells[i] == ' ')
                {
                    free.Add(i);
                }
            }
            return free.ToArray();
        }
    }

    public bool TryPlace(int index, char mark)
    {
        if (index is < 0 or > 8 || (mark != 'X' && mark != 'O') || _cells[index] != ' ')
            return false;
        _cells[index] = mark;
        return true;
    }

    public char? GetWinner()
    {
        for (int l = 0; l < WinLines.Length; l++)
        {
            int[] line = WinLines[l];
            char first = _cells[line[0]];
            if (first == ' ')
            {
                continue;
            }
            if (_cells[line[1]] == first && _cells[line[2]] == first)
            {
                return first;
            }
        }
        return null;
    }

    public bool IsDraw()
    {
        for (int i = 0; i < 9; i++)
        {
            if (_cells[i] == ' ')
            {
                return false;
            }
        }
        return GetWinner() is null;
    }

    public bool IsGameOver()
    {
        return GetWinner() is not null || IsDraw();
    }

    public void Render()
    {
        string Cell(int i)
        {
            if (_cells[i] == ' ')
            {
                return (i + 1).ToString();
            }
            return _cells[i].ToString();
        }
        Console.WriteLine($" {Cell(0)} | {Cell(1)} | {Cell(2)} ");
        Console.WriteLine("---+---+---");
        Console.WriteLine($" {Cell(3)} | {Cell(4)} | {Cell(5)} ");
        Console.WriteLine("---+---+---");
        Console.WriteLine($" {Cell(6)} | {Cell(7)} | {Cell(8)} ");
    }
}
