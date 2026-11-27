using System.IO;
using System.Windows.Media;
using Tetris.Core.Gameplay;

namespace Tetris.Desktop.Presentation;

internal sealed class GameAudio : IDisposable
{
    private const double MusicVolume = 0.32;
    private const double MusicVolumeDuringAchievement = 0.10;
    private static readonly double[] MusicSpeeds = [1.0, 1.1, 1.2, 1.3, 1.4];

    private readonly MediaPlayer _music = OpenTrack("game  loop.mp3", MusicVolume);
    private readonly MediaPlayer[] _clicks = Enumerable.Range(0, 4)
        .Select(_ => OpenTrack("click.mp3", 0.50)).ToArray();
    private readonly MediaPlayer _achievement = OpenTrack("achievement.mp3", 0.90);
    private readonly MediaPlayer _gameOver = OpenTrack("game over.mp3", 0.60);

    private bool _enabled = true;
    private MusicState _musicState;
    private int _nextClick;

    private enum MusicState
    {
        Stopped,
        Paused,
        Playing
    }

    public GameAudio()
    {
        _music.MediaEnded += OnMusicEnded;
        _achievement.MediaEnded += OnAchievementEnded;
        _achievement.MediaFailed += OnAchievementEnded;
    }

    public void Update(bool enabled, GamePhase phase, int level)
    {
        if (_enabled != enabled)
        {
            _enabled = enabled;
            _music.IsMuted = !enabled;
            _achievement.IsMuted = !enabled;
            _gameOver.IsMuted = !enabled;
            foreach (var click in _clicks)
            {
                click.IsMuted = !enabled;
                if (!enabled)
                    click.Stop();
            }
            if (!enabled)
            {
                _achievement.Stop();
                _gameOver.Stop();
                _music.Volume = MusicVolume;
            }
        }

        var speed = MusicSpeeds[level - 1];
        if (_music.SpeedRatio != speed)
            _music.SpeedRatio = speed;

        var requested = phase switch
        {
            GamePhase.Running when enabled => MusicState.Playing,
            GamePhase.Running or GamePhase.Paused => MusicState.Paused,
            _ => MusicState.Stopped
        };
        if (requested == _musicState)
            return;

        _musicState = requested;
        switch (requested)
        {
            case MusicState.Playing:
                _music.Play();
                break;
            case MusicState.Paused:
                _music.Pause();
                break;
            case MusicState.Stopped:
                _music.Stop();
                _music.Volume = MusicVolume;
                break;
        }
    }

    public void PlayClick()
    {
        PlayEffect(_clicks[_nextClick]);
        _nextClick = (_nextClick + 1) % _clicks.Length;
    }

    public void PlayAchievement()
    {
        if (_enabled && _achievement.NaturalDuration.HasTimeSpan)
            _music.Volume = MusicVolumeDuringAchievement;

        PlayEffect(_achievement);
    }

    public void PlayGameOver() => PlayEffect(_gameOver);

    private void PlayEffect(MediaPlayer player)
    {
        if (!_enabled)
            return;

        player.Position = TimeSpan.Zero;
        player.Play();
    }

    private void OnMusicEnded(object? sender, EventArgs e)
    {
        if (_musicState == MusicState.Stopped)
            return;

        _music.Position = TimeSpan.Zero;
        if (_musicState == MusicState.Playing)
            _music.Play();
    }

    private void OnAchievementEnded(object? sender, EventArgs e) => _music.Volume = MusicVolume;

    public void Dispose()
    {
        _music.MediaEnded -= OnMusicEnded;
        _achievement.MediaEnded -= OnAchievementEnded;
        _achievement.MediaFailed -= OnAchievementEnded;
        _music.Close();
        foreach (var click in _clicks)
            click.Close();
        _achievement.Close();
        _gameOver.Close();
    }

    private static MediaPlayer OpenTrack(string filename, double volume)
    {
        var player = new MediaPlayer { Volume = volume };
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "music", filename);
        player.Open(new Uri(path, UriKind.Absolute));
        return player;
    }
}
