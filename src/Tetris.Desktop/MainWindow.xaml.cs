using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Tetris.Core.Gameplay;
using Tetris.Desktop.Application;
using Tetris.Desktop.Presentation;
using Tetris.Desktop.Storage;

namespace Tetris.Desktop;

public partial class MainWindow : Window
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    private readonly GameController _controller = new(new GameSession(), new InMemoryRecordStore());
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private nint _windowHandle;
    private long _lastUpdate;

    public MainWindow()
    {
        InitializeComponent();
        _timer.Tick += OnTick;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        ComponentDispatcher.ThreadFilterMessage += OnThreadMessage;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LevelOne.IsChecked = true;
        RecentTab.IsChecked = true;
        UpdateSchemeChoices();
        _lastUpdate = Stopwatch.GetTimestamp();
        _timer.Start();
        Refresh(moveFocus: true);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ComponentDispatcher.ThreadFilterMessage -= OnThreadMessage;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnStart(object sender, RoutedEventArgs e)
    {
        _controller.Start();
        _lastUpdate = Stopwatch.GetTimestamp();
        Refresh(moveFocus: true);
    }

    private void OnPause(object sender, RoutedEventArgs e) => Pause();
    private void OnDeactivated(object? sender, EventArgs e) => Pause();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _controller.Screen != AppScreen.Game)
            return;

        e.Handled = true;
        if (e.IsRepeat)
            return;

        if (_controller.Snapshot.Phase == GamePhase.Paused)
            Resume();
        else
            Pause();
    }

    private void OnThreadMessage(ref MSG message, ref bool handled)
    {
        if (handled || message.hwnd != _windowHandle || message.message != KeyboardControls.KeyDownMessage
            || !_controller.IsGameRunning
            || (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0)
            return;

        if (KeyboardControls.GetMovement(_controller.Scheme, message.lParam) is not { } command)
            return;

        handled = true;
        if (KeyboardControls.IsRepeat(message.lParam))
            return;

        var oldSnapshot = _controller.Snapshot;
        var oldScreen = _controller.Screen;
        UpdateClock();
        _controller.ApplyMovement(command);
        if (oldScreen != _controller.Screen || !ReferenceEquals(oldSnapshot, _controller.Snapshot))
            Refresh(moveFocus: oldScreen != _controller.Screen);
    }

    private void Pause()
    {
        if (!_controller.IsGameRunning)
            return;

        UpdateClock();
        _controller.Pause();
        Refresh(moveFocus: true);
    }

    private void OnResume(object sender, RoutedEventArgs e) => Resume();

    private void Resume()
    {
        _controller.Resume();
        _lastUpdate = Stopwatch.GetTimestamp();
        Refresh(moveFocus: true);
    }

    private void OnReturnToMenu(object sender, RoutedEventArgs e)
    {
        _controller.ReturnToMenu();
        Refresh(moveFocus: true);
    }

    private void OnOpenRecords(object sender, RoutedEventArgs e)
    {
        _controller.ShowRecords();
        RecentTab.IsChecked = true;
        Refresh(moveFocus: true);
    }

    private void OnCloseRecords(object sender, RoutedEventArgs e)
    {
        _controller.CloseRecords();
        Refresh(moveFocus: true);
    }

    private void OnRecordTabChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded && _controller.Screen == AppScreen.Records)
            RefreshRecords();
    }

    private void OnLevelSelected(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string level })
        {
            _controller.InitialLevel = int.Parse(level, CultureInfo.InvariantCulture);
            if (IsLoaded)
                Refresh();
        }
    }

    private void OnSchemeSelected(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag }
            && Enum.TryParse<ControlScheme>(tag, out var scheme)
            && _controller.Scheme != scheme)
        {
            _controller.Scheme = scheme;
            UpdateSchemeChoices();
        }
    }

    private void UpdateSchemeChoices()
    {
        if (_controller.Scheme == ControlScheme.Arrows)
        {
            MenuArrows.IsChecked = true;
            PauseArrows.IsChecked = true;
        }
        else
        {
            MenuHomeRow.IsChecked = true;
            PauseHomeRow.IsChecked = true;
        }
    }

    private void OnToggleSound(object sender, RoutedEventArgs e)
    {
        _controller.ToggleSound();
        RefreshSoundButton();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var oldSnapshot = _controller.Snapshot;
        var oldScreen = _controller.Screen;
        UpdateClock();
        if (oldScreen != _controller.Screen || !ReferenceEquals(oldSnapshot, _controller.Snapshot))
            Refresh(moveFocus: oldScreen != _controller.Screen);
    }

    private void UpdateClock()
    {
        var now = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_lastUpdate, now);
        _lastUpdate = now;
        _controller.Advance(elapsed);
    }

    private void Refresh(bool moveFocus = false)
    {
        var screen = _controller.Screen;
        var snapshot = _controller.Snapshot;
        var paused = screen == AppScreen.Game && snapshot.Phase == GamePhase.Paused;

        MenuScreen.Visibility = VisibilityFor(screen == AppScreen.Menu);
        GameScreen.Visibility = VisibilityFor(screen == AppScreen.Game);
        GameSidebar.IsEnabled = !paused;
        PauseLayer.Visibility = VisibilityFor(paused);
        ResultsScreen.Visibility = VisibilityFor(screen == AppScreen.Results);
        RecordsScreen.Visibility = VisibilityFor(screen == AppScreen.Records);
        RefreshSoundButton();

        switch (screen)
        {
            case AppScreen.Menu:
                MenuField.ShowBoard(MenuPreview.Snapshot, _controller.InitialLevel);
                MenuLevelLabel.Text = $"Уровень {_controller.InitialLevel}";
                break;
            case AppScreen.Game:
                GameField.ShowBoard(snapshot, snapshot.Level);
                PiecePreview.ShowPiece(snapshot.NextPiece, snapshot.Level);
                ScoreLabel.Text = snapshot.Score.ToString("N0", Russian);
                LevelLabel.Text = snapshot.Level.ToString(Russian);
                ClearedLabel.Text = snapshot.ClearedLines.ToString(Russian);
                GameControlsLabel.Text = _controller.Scheme == ControlScheme.Arrows
                    ? "← / → — движение\nA / D — поворот"
                    : "J / ; — движение\nK / L — поворот";
                break;
            case AppScreen.Results when snapshot.Result is { } result:
                ResultScoreLabel.Text = result.Score.ToString("N0", Russian);
                ResultTimeLabel.Text = FormatTime(result.ActiveTime);
                break;
            case AppScreen.Records:
                RefreshRecords();
                break;
        }

        if (moveFocus)
        {
            var target = screen switch
            {
                AppScreen.Menu => StartButton,
                AppScreen.Game when paused => ResumeButton,
                AppScreen.Game => (UIElement)Root,
                AppScreen.Results => ResultRecordsButton,
                AppScreen.Records => RecordsBackButton,
                _ => Root
            };
            target.Focus();
        }
    }

    private void RefreshSoundButton() =>
        SoundButton.Content = _controller.IsSoundEnabled ? "Звук: включён" : "Звук: выключен";

    private void RefreshRecords()
    {
        var source = TopTab.IsChecked == true ? _controller.TopRecords : _controller.RecentRecords;
        RecordsTable.ItemsSource = source.Select(entry => new RecordRow(
            entry.Date.ToString("dd.MM.yyyy HH:mm", Russian),
            entry.Result.Score.ToString("N0", Russian),
            FormatTime(entry.Result.ActiveTime),
            entry.Result.InitialLevel)).ToArray();
        RecordsEmpty.Visibility = VisibilityFor(source.Count == 0);
    }

    private static Visibility VisibilityFor(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private static string FormatTime(TimeSpan duration) =>
        $"{(long)duration.TotalMinutes:00}:{duration.Seconds:00}";

    private sealed record RecordRow(string Date, string Score, string ActiveTime, int InitialLevel);
}
