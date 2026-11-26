using System.IO;
using Tetris.Core.Gameplay;
using Tetris.Core.Records;

namespace Tetris.Desktop.Application;

internal enum AppScreen
{
    Menu,
    Game,
    Results,
    Records
}

internal enum ControlScheme
{
    Arrows,
    HomeRow
}

internal sealed class GameController(IGameSession session, IRecordStore records)
{
    private readonly Queue<RecordEntry> _pendingRecords = new();
    private AppScreen _recordsReturnScreen;
    private Guid _gameId;

    public AppScreen Screen { get; private set; }
    public int InitialLevel { get; set; } = 1;
    public ControlScheme Scheme { get; set; }
    public bool IsSoundEnabled { get; private set; } = true;
    public GameSnapshot Snapshot => session.Snapshot;
    public bool IsGameRunning => Screen == AppScreen.Game && Snapshot.Phase == GamePhase.Running;
    public IReadOnlyList<RecordEntry> RecentRecords => records.Recent;
    public IReadOnlyList<RecordEntry> TopRecords => records.Top;
    public bool HasPendingRecords => _pendingRecords.Count != 0;
    public bool RecordsLoadFailed { get; private set; }

    public void Start()
    {
        _gameId = Guid.NewGuid();
        session.Start(InitialLevel);
        Screen = AppScreen.Game;
    }

    public void Pause()
    {
        if (IsGameRunning)
            session.Apply(GameCommand.Pause);
    }

    public void Resume()
    {
        if (Screen == AppScreen.Game && Snapshot.Phase == GamePhase.Paused)
            session.Apply(GameCommand.Resume);
    }

    public void ReturnToMenu()
    {
        if (Screen == AppScreen.Game && Snapshot.Phase == GamePhase.Paused)
            session.Apply(GameCommand.Abandon);

        Screen = AppScreen.Menu;
    }

    public void ShowRecords()
    {
        _recordsReturnScreen = Screen;
        Screen = AppScreen.Records;
        LoadRecords();
    }

    public void CloseRecords() => Screen = _recordsReturnScreen;

    public void RetryRecords()
    {
        SavePendingRecords();
        if (Screen == AppScreen.Records)
            LoadRecords();
    }

    public void ToggleSound() => IsSoundEnabled = !IsSoundEnabled;

    public void ApplyCommand(GameCommand command)
    {
        if (!IsGameRunning)
            return;

        session.Apply(command);
        FinishIfLost();
    }

    public void Advance(TimeSpan elapsed)
    {
        if (!IsGameRunning)
            return;

        session.Advance(elapsed);
        FinishIfLost();
    }

    private void FinishIfLost()
    {
        if (Snapshot.Phase == GamePhase.Lost && Snapshot.Result is { } result)
        {
            Screen = AppScreen.Results;
            _pendingRecords.Enqueue(new RecordEntry(_gameId, DateTimeOffset.Now, result));
            SavePendingRecords();
        }
    }

    private void SavePendingRecords()
    {
        while (_pendingRecords.TryPeek(out var entry))
        {
            try
            {
                records.Save(entry);
                _pendingRecords.Dequeue();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return;
            }
        }
    }

    private void LoadRecords()
    {
        try
        {
            records.Reload();
            RecordsLoadFailed = false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            RecordsLoadFailed = true;
        }
    }
}
