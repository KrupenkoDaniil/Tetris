namespace Tetris.Core.Gameplay;

public sealed class Board
{
    private readonly bool[,] _occupied = new bool[GameSnapshot.BoardRows, GameSnapshot.BoardColumns];

    public Board() : this([])
    {
    }

    public Board(IEnumerable<BoardCell> lockedCells)
    {
        var cells = lockedCells.ToArray();
        foreach (var cell in cells)
        {
            if (!IsInside(cell.Column, cell.Row))
                throw new ArgumentException("Клетка находится за пределами поля.", nameof(lockedCells));
            if (_occupied[cell.Row, cell.Column])
                throw new ArgumentException("В поле есть повторяющаяся клетка.", nameof(lockedCells));

            _occupied[cell.Row, cell.Column] = true;
        }

        LockedCells = Array.AsReadOnly(cells);
    }

    public IReadOnlyList<BoardCell> LockedCells { get; }

    public bool CanPlace(PieceSnapshot piece)
    {
        foreach (var cell in GetCells(piece))
        {
            if (!IsInside(cell.Column, cell.Row) || _occupied[cell.Row, cell.Column])
                return false;
        }

        return true;
    }

    internal (Board Field, int ClearedLines) LockAndClear(PieceSnapshot piece)
    {
        var cells = LockedCells.Concat(GetCells(piece)).ToArray();
        var rowSizes = new int[GameSnapshot.BoardRows];
        foreach (var cell in cells)
            rowSizes[cell.Row]++;

        var rowDrop = new int[GameSnapshot.BoardRows];
        var clearedLines = 0;
        for (var row = GameSnapshot.BoardRows - 1; row >= 0; row--)
        {
            if (rowSizes[row] == GameSnapshot.BoardColumns)
                clearedLines++;
            else
                rowDrop[row] = clearedLines;
        }

        if (clearedLines == 0)
            return (new Board(cells), 0);

        var remaining = cells
            .Where(cell => rowSizes[cell.Row] < GameSnapshot.BoardColumns)
            .Select(cell => cell with { Row = cell.Row + rowDrop[cell.Row] });
        return (new Board(remaining), clearedLines);
    }

    private static IEnumerable<BoardCell> GetCells(PieceSnapshot piece)
    {
        for (var row = 0; row < piece.Mask.Count; row++)
        {
            for (var column = 0; column < piece.Mask[row].Length; column++)
            {
                if (piece.Mask[row][column] == '#')
                    yield return new BoardCell(piece.Column + column, piece.Row + row, piece.Type);
            }
        }
    }

    private static bool IsInside(int column, int row) =>
        column >= 0 && column < GameSnapshot.BoardColumns &&
        row >= 0 && row < GameSnapshot.BoardRows;
}
