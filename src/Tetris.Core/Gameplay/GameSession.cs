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
    Abandon
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
    PieceSnapshot NextPiece,
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
