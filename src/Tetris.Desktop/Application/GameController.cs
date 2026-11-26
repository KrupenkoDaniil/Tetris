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
    private AppScreen _recordsReturnScreen;
    private Guid _gameId;

    public AppScreen Screen { get; private set; }
    public int InitialLevel { get; set; } = 1;
    public ControlScheme Scheme { get; set; }
    public bool IsSoundEnabled { get; private set; } = true;
    public GameSnapshot Snapshot => session.Snapshot;
    public IReadOnlyList<RecordEntry> RecentRecords => records.Recent;
    public IReadOnlyList<RecordEntry> TopRecords => records.Top;

    public void Start()
    {
        _gameId = Guid.NewGuid();
        session.Start(InitialLevel);
        Screen = AppScreen.Game;
    }

    public void Pause()
    {
        if (Screen == AppScreen.Game && Snapshot.Phase == GamePhase.Running)
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
    }

    public void CloseRecords() => Screen = _recordsReturnScreen;

    public void ToggleSound() => IsSoundEnabled = !IsSoundEnabled;

    public void Advance(TimeSpan elapsed)
    {
        if (Screen != AppScreen.Game || Snapshot.Phase != GamePhase.Running)
            return;

        session.Advance(elapsed);
        if (Snapshot.Phase == GamePhase.Lost && Snapshot.Result is { } result)
        {
            records.Save(new RecordEntry(_gameId, DateTimeOffset.Now, result));
            Screen = AppScreen.Results;
        }
    }
}
