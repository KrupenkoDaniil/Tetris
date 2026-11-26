using Tetris.Core.Gameplay;

namespace Tetris.Desktop.Application;

internal sealed class DemoGameSession : IGameSession
{
    private static readonly TimeSpan DemoDuration = TimeSpan.FromSeconds(15);
    private static readonly IReadOnlyList<BoardCell> SampleField = CreateSampleField();
    private static readonly PieceSnapshot SampleActive = new(2, 5, 6, Array.AsReadOnly(new[] { ".#.", ".#.", "###" }));
    private static readonly PieceSnapshot SampleNext = new(13, 0, 0, Array.AsReadOnly(new[] { ".#.", ".##", "##." }));

    private TimeSpan _elapsed;

    public GameSnapshot Snapshot { get; private set; } = CreateSnapshot(GamePhase.Ready, 1);

    public void Start(int initialLevel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialLevel, 5);
        _elapsed = TimeSpan.Zero;
        Snapshot = CreateSnapshot(GamePhase.Running, initialLevel);
    }

    public void Apply(GameCommand command)
    {
        var phase = (Snapshot.Phase, command) switch
        {
            (GamePhase.Running, GameCommand.Pause) => GamePhase.Paused,
            (GamePhase.Paused, GameCommand.Resume) => GamePhase.Running,
            (GamePhase.Running or GamePhase.Paused, GameCommand.Abandon) => GamePhase.Abandoned,
            _ => Snapshot.Phase
        };

        if (phase != Snapshot.Phase)
            Snapshot = Snapshot with { Phase = phase };
    }

    public void Advance(TimeSpan elapsed)
    {
        if (Snapshot.Phase != GamePhase.Running || elapsed <= TimeSpan.Zero)
            return;

        _elapsed += elapsed;
        if (_elapsed >= DemoDuration)
        {
            var result = new GameResult(1200, DemoDuration, Snapshot.Level);
            Snapshot = Snapshot with { Phase = GamePhase.Lost, Score = result.Score, Result = result };
        }
    }

    private static GameSnapshot CreateSnapshot(GamePhase phase, int level) =>
        new(phase, level, 0, 0, SampleField, SampleActive, SampleNext);

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
