namespace Tetris.Core.Gameplay;

public enum GamePhase
{
    Ready,
    Running,
    Paused,
    Lost,
    Abandoned
}

public enum GameCommand
{
    Pause,
    Resume,
    Abandon,
    MoveLeft,
    MoveRight,
    RotateCounterclockwise,
    RotateClockwise
}

public readonly record struct BoardCell(int Column, int Row, int PieceType);

public sealed record PieceSnapshot(int Type, int Column, int Row, IReadOnlyList<string> Mask);

public sealed record GameResult(int Score, TimeSpan ActiveTime, int InitialLevel);

public sealed record GameSnapshot(
    GamePhase Phase,
    int Level,
    int Score,
    int ClearedLines,
    IReadOnlyList<BoardCell> LockedCells,
    PieceSnapshot? ActivePiece,
    PieceSnapshot? NextPiece,
    GameResult? Result = null)
{
    public const int BoardColumns = 15;
    public const int BoardRows = 30;
}

public interface IGameSession
{
    GameSnapshot Snapshot { get; }

    void Start(int initialLevel);
    void Apply(GameCommand command);
    void Advance(TimeSpan elapsed);
}

public sealed class GameSession : IGameSession
{
    private static readonly (int Column, int Row)[] RotationOffsets =
        [(0, 0), (-1, 0), (1, 0), (-2, 0), (2, 0), (0, -1)];

    private readonly IFigureSource _figures;
    private Board _board = new();

    public GameSession() : this(new RandomFigureSource())
    {
    }

    public GameSession(IFigureSource figures)
    {
        ArgumentNullException.ThrowIfNull(figures);
        _figures = figures;
        Snapshot = new GameSnapshot(GamePhase.Ready, 1, 0, 0, _board.LockedCells, null, null);
    }

    public GameSnapshot Snapshot { get; private set; }

    public void Start(int initialLevel)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialLevel, 5);

        var active = CreatePiece(_figures.NextType());
        var next = CreatePiece(_figures.NextType());
        _board = new Board();
        Snapshot = new GameSnapshot(GamePhase.Running, initialLevel, 0, 0, _board.LockedCells, active, next);
    }

    public void Apply(GameCommand command)
    {
        switch (command)
        {
            case GameCommand.Pause when Snapshot.Phase == GamePhase.Running:
                Snapshot = Snapshot with { Phase = GamePhase.Paused };
                break;
            case GameCommand.Resume when Snapshot.Phase == GamePhase.Paused:
                Snapshot = Snapshot with { Phase = GamePhase.Running };
                break;
            case GameCommand.Abandon when Snapshot.Phase is GamePhase.Running or GamePhase.Paused:
                Snapshot = Snapshot with { Phase = GamePhase.Abandoned, ActivePiece = null, NextPiece = null };
                break;
            default:
                if (Snapshot.Phase == GamePhase.Running && Snapshot.ActivePiece is { } active)
                    ApplyMovement(active, command);
                break;
        }
    }

    public void Advance(TimeSpan elapsed)
    {
        // The next development stage adds gravity and the lock timer here.
    }

    private void ApplyMovement(PieceSnapshot piece, GameCommand command)
    {
        var candidate = command switch
        {
            GameCommand.MoveLeft => piece with { Column = piece.Column - 1 },
            GameCommand.MoveRight => piece with { Column = piece.Column + 1 },
            GameCommand.RotateCounterclockwise => FindRotation(piece, clockwise: false),
            GameCommand.RotateClockwise => FindRotation(piece, clockwise: true),
            _ => null
        };

        if (candidate is not null && _board.CanPlace(candidate))
            Snapshot = Snapshot with { ActivePiece = candidate };
    }

    private PieceSnapshot? FindRotation(PieceSnapshot piece, bool clockwise)
    {
        var rotatedMask = RotateMask(piece.Mask, clockwise);
        foreach (var (column, row) in RotationOffsets)
        {
            var candidate = piece with
            {
                Column = piece.Column + column,
                Row = piece.Row + row,
                Mask = rotatedMask
            };
            if (_board.CanPlace(candidate))
                return candidate;
        }

        return null;
    }

    private static PieceSnapshot CreatePiece(int type)
    {
        var mask = PieceCatalog.GetMask(type);
        var column = (GameSnapshot.BoardColumns - mask[0].Length) / 2;
        return new PieceSnapshot(type, column, 0, mask);
    }

    private static IReadOnlyList<string> RotateMask(IReadOnlyList<string> mask, bool clockwise)
    {
        var height = mask.Count;
        var width = mask[0].Length;
        var rotated = new string[width];

        for (var row = 0; row < width; row++)
        {
            var line = new char[height];
            for (var column = 0; column < height; column++)
                line[column] = clockwise ? mask[height - 1 - column][row] : mask[column][width - 1 - row];

            rotated[row] = new string(line);
        }

        return Array.AsReadOnly(rotated);
    }

    private sealed class RandomFigureSource : IFigureSource
    {
        public int NextType() => Random.Shared.Next(1, PieceCatalog.Count + 1);
    }
}
