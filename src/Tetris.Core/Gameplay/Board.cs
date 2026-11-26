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

            _occupied[cell.Row, cell.Column] = true;
        }

        LockedCells = Array.AsReadOnly(cells);
    }

    public IReadOnlyList<BoardCell> LockedCells { get; }

    public bool CanPlace(PieceSnapshot piece)
    {
        for (var row = 0; row < piece.Mask.Count; row++)
        {
            for (var column = 0; column < piece.Mask[row].Length; column++)
            {
                if (piece.Mask[row][column] != '#')
                    continue;

                var boardColumn = piece.Column + column;
                var boardRow = piece.Row + row;
                if (!IsInside(boardColumn, boardRow) || _occupied[boardRow, boardColumn])
                    return false;
            }
        }

        return true;
    }

    private static bool IsInside(int column, int row) =>
        column >= 0 && column < GameSnapshot.BoardColumns &&
        row >= 0 && row < GameSnapshot.BoardRows;
}
