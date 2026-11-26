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
    StartMoveLeft,
    StopMoveLeft,
    StartMoveRight,
    StopMoveRight,
    RotateCounterclockwise,
    RotateClockwise,
    StartDash,
    StopDash
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
    public const int BoardColumns = 10;
    public const int BoardRows = 20;
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
    private static readonly TimeSpan DashInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan LockDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan MoveRepeatDelay = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan MoveRepeatInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan DashBonusInterval = TimeSpan.FromMilliseconds(200);
    private const int MaximumDashBonus = 50;
    private const int LinesPerLevel = 10;
    private static readonly TimeSpan[] FallIntervals =
    [
        TimeSpan.FromMilliseconds(900),
        TimeSpan.FromMilliseconds(700),
        TimeSpan.FromMilliseconds(520),
        TimeSpan.FromMilliseconds(360),
        TimeSpan.FromMilliseconds(240)
    ];

    private static readonly (int Column, int Row)[] RotationOffsets =
        [(0, 0), (-1, 0), (1, 0), (-2, 0), (2, 0), (0, -1)];

    private readonly IFigureSource _figures;
    private Board _board = new();
    private int _initialLevel;
    private bool _isDashing;
    private HorizontalDirection _heldDirections;
    private HorizontalDirection _repeatDirection;
    private TimeSpan _activeTime;
    private TimeSpan _dashTimeSinceScore;
    private TimeSpan _timeSinceFallStep;
    private TimeSpan _timeOnSupport;
    private TimeSpan _timeUntilRepeat;

    [Flags]
    private enum HorizontalDirection
    {
        None = 0,
        Left = 1,
        Right = 2
    }

    public GameSession() : this(new WeightedFigureSource())
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

        var active = DrawPiece();
        var next = DrawPiece();
        _board = new Board();
        _initialLevel = initialLevel;
        ClearHeldControls();
        _activeTime = TimeSpan.Zero;
        _dashTimeSinceScore = TimeSpan.Zero;
        _timeSinceFallStep = TimeSpan.Zero;
        _timeOnSupport = TimeSpan.Zero;
        Snapshot = new GameSnapshot(GamePhase.Running, initialLevel, 0, 0, _board.LockedCells, active, next);
    }

    public void Apply(GameCommand command)
    {
        switch (command)
        {
            case GameCommand.Pause when Snapshot.Phase == GamePhase.Running:
                ClearHeldControls();
                Snapshot = Snapshot with { Phase = GamePhase.Paused };
                break;
            case GameCommand.Resume when Snapshot.Phase == GamePhase.Paused:
                Snapshot = Snapshot with { Phase = GamePhase.Running };
                break;
            case GameCommand.Abandon when Snapshot.Phase is GamePhase.Running or GamePhase.Paused:
                ClearHeldControls();
                Snapshot = Snapshot with { Phase = GamePhase.Abandoned, ActivePiece = null, NextPiece = null };
                break;
            case GameCommand.StartMoveLeft when Snapshot.Phase == GamePhase.Running:
                StartHorizontalMove(HorizontalDirection.Left);
                break;
            case GameCommand.StartMoveRight when Snapshot.Phase == GamePhase.Running:
                StartHorizontalMove(HorizontalDirection.Right);
                break;
            case GameCommand.StopMoveLeft:
                StopHorizontalMove(HorizontalDirection.Left);
                break;
            case GameCommand.StopMoveRight:
                StopHorizontalMove(HorizontalDirection.Right);
                break;
            case GameCommand.StartDash when Snapshot.Phase == GamePhase.Running:
                _isDashing = true;
                Advance(TimeSpan.Zero);
                break;
            case GameCommand.StopDash:
                _isDashing = false;
                break;
            default:
                if (Snapshot.Phase == GamePhase.Running && Snapshot.ActivePiece is { } active
                    && ApplyMovement(active, command))
                    Advance(TimeSpan.Zero);
                break;
        }
    }

    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
            return;

        while (Snapshot.Phase == GamePhase.Running && Snapshot.ActivePiece is { } active)
        {
            var below = active with { Row = active.Row + 1 };
            var supported = !_board.CanPlace(below);
            var fallInterval = _isDashing ? DashInterval : FallIntervals[Snapshot.Level - 1];
            var untilVerticalAction = supported ? LockDelay - _timeOnSupport : fallInterval - _timeSinceFallStep;
            if (untilVerticalAction < TimeSpan.Zero)
                untilVerticalAction = TimeSpan.Zero;

            var repeating = _repeatDirection != HorizontalDirection.None;
            var untilAction = repeating && _timeUntilRepeat < untilVerticalAction
                ? _timeUntilRepeat
                : untilVerticalAction;

            var spent = elapsed < untilAction ? elapsed : untilAction;
            _activeTime += spent;
            if (_isDashing && !supported)
                _dashTimeSinceScore += spent;
            _timeSinceFallStep += spent;
            _timeOnSupport = supported ? _timeOnSupport + spent : TimeSpan.Zero;
            if (repeating)
                _timeUntilRepeat -= spent;
            elapsed -= spent;

            if (spent < untilAction)
                return;

            if (spent == untilVerticalAction)
            {
                if (supported)
                {
                    LockPiece(active);
                }
                else
                {
                    _timeSinceFallStep = TimeSpan.Zero;
                    Snapshot = Snapshot with { ActivePiece = below };
                }
            }

            if (_repeatDirection != HorizontalDirection.None && _timeUntilRepeat == TimeSpan.Zero
                && Snapshot.ActivePiece is { } current)
            {
                _timeUntilRepeat = MoveRepeatInterval;
                ApplyMovement(current, MovementFor(_repeatDirection));
            }
        }
    }

    private void StartHorizontalMove(HorizontalDirection direction)
    {
        if ((_heldDirections & direction) != 0)
            return;

        _heldDirections |= direction;
        _repeatDirection = direction;
        _timeUntilRepeat = MoveRepeatDelay;
        if (Snapshot.ActivePiece is { } active && ApplyMovement(active, MovementFor(direction)))
            Advance(TimeSpan.Zero);
    }

    private void StopHorizontalMove(HorizontalDirection direction)
    {
        _heldDirections &= ~direction;
        if (_repeatDirection == direction)
        {
            _repeatDirection = _heldDirections;
            _timeUntilRepeat = MoveRepeatDelay;
        }
    }

    private void ClearHeldControls()
    {
        _isDashing = false;
        _heldDirections = HorizontalDirection.None;
        _repeatDirection = HorizontalDirection.None;
        _timeUntilRepeat = TimeSpan.Zero;
    }

    private static GameCommand MovementFor(HorizontalDirection direction) =>
        direction == HorizontalDirection.Left ? GameCommand.MoveLeft : GameCommand.MoveRight;

    private bool ApplyMovement(PieceSnapshot piece, GameCommand command)
    {
        var candidate = command switch
        {
            GameCommand.MoveLeft => piece with { Column = piece.Column - 1 },
            GameCommand.MoveRight => piece with { Column = piece.Column + 1 },
            GameCommand.RotateCounterclockwise => FindRotation(piece, clockwise: false),
            GameCommand.RotateClockwise => FindRotation(piece, clockwise: true),
            _ => null
        };

        if (candidate is null || !_board.CanPlace(candidate))
            return false;

        Snapshot = Snapshot with { ActivePiece = candidate };
        if (_board.CanPlace(candidate with { Row = candidate.Row + 1 }))
            _timeOnSupport = TimeSpan.Zero;

        return true;
    }

    private void LockPiece(PieceSnapshot piece)
    {
        var (board, clearedLines) = _board.LockAndClear(piece);
        var score = Snapshot.Score + ScoreForClear(clearedLines);
        var totalLines = Snapshot.ClearedLines + clearedLines;
        var level = Math.Min(FallIntervals.Length, _initialLevel + totalLines / LinesPerLevel);
        var next = Snapshot.NextPiece;
        var canSpawn = next is not null && board.CanPlace(next);
        var preview = canSpawn ? DrawPiece() : null;

        _board = board;
        if (clearedLines > 0 || !canSpawn)
            _dashTimeSinceScore = TimeSpan.Zero;
        _timeSinceFallStep = TimeSpan.Zero;
        _timeOnSupport = TimeSpan.Zero;
        if (!canSpawn)
            ClearHeldControls();

        Snapshot = Snapshot with
        {
            Phase = canSpawn ? GamePhase.Running : GamePhase.Lost,
            Level = level,
            Score = score,
            ClearedLines = totalLines,
            LockedCells = board.LockedCells,
            ActivePiece = canSpawn ? next : null,
            NextPiece = preview,
            Result = canSpawn ? null : new GameResult(score, _activeTime, _initialLevel)
        };
    }

    private int ScoreForClear(int clearedLines)
    {
        if (clearedLines == 0)
            return 0;

        var lineMultiplier = clearedLines == 4 ? 2 : 1;
        var lineScore = 25 * (Snapshot.Level + 3) * clearedLines * lineMultiplier;
        var dashBonus = Math.Min(MaximumDashBonus, _dashTimeSinceScore.Ticks / DashBonusInterval.Ticks);
        return lineScore + (int)dashBonus;
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

    private PieceSnapshot DrawPiece()
    {
        var type = _figures.NextType();
        var mask = PieceCatalog.GetMask(type, _figures.NextIsMirrored(type));
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

    private sealed class WeightedFigureSource : IFigureSource
    {
        private static readonly int[] Tickets = Enumerable.Range(1, PieceCatalog.Count)
            .SelectMany(type => Enumerable.Repeat(type, WeightFor(type)))
            .ToArray();

        public int NextType() => Tickets[Random.Shared.Next(Tickets.Length)];

        public bool NextIsMirrored(int type) => PieceCatalog.HasMirroredVariant(type) && Random.Shared.Next(2) == 1;

        private static int WeightFor(int type) => type switch
        {
            8 or 9 or 10 or 12 => 2,
            13 or 14 => 1,
            _ => 4
        };
    }
}
