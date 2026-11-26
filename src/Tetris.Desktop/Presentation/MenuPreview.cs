using Tetris.Core.Gameplay;

namespace Tetris.Desktop.Presentation;

internal static class MenuPreview
{
    public static GameSnapshot Snapshot { get; } = new(
        GamePhase.Ready, 1, 0, 0, CreateSampleField(),
        new PieceSnapshot(2, 3, 4, PieceCatalog.GetMask(2)), null);

    private static IReadOnlyList<BoardCell> CreateSampleField()
    {
        string[] rows =
        [
            ".......22.",
            "11.....322",
            "11....3332",
            "112...3322",
            "1122..1322",
            "1112.41322",
            "111224.332"
        ];

        var cells = new List<BoardCell>();
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                if (rows[row][column] != '.')
                    cells.Add(new BoardCell(column, GameSnapshot.BoardRows - rows.Length + row, rows[row][column] - '0'));
            }
        }

        return cells.AsReadOnly();
    }
}
