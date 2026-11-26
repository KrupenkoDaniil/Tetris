using Tetris.Core.Gameplay;

namespace Tetris.Desktop.Presentation;

internal static class MenuPreview
{
    public static GameSnapshot Snapshot { get; } = new(
        GamePhase.Ready, 1, 0, 0, CreateSampleField(),
        new PieceSnapshot(2, 5, 6, PieceCatalog.GetMask(2)), null);

    private static IReadOnlyList<BoardCell> CreateSampleField()
    {
        string[] rows =
        [
            "............22.",
            "...........322.",
            "11.........332.",
            "11........33322",
            "112........3322",
            "1122...44.13322",
            "1112..444.11322",
            "11122.443311332",
            "11122344.311332"
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
