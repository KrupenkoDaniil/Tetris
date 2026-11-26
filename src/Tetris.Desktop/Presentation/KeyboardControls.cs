using Tetris.Core.Gameplay;
using Tetris.Desktop.Application;

namespace Tetris.Desktop.Presentation;

internal static class KeyboardControls
{
    public const int KeyDownMessage = 0x0100;
    public const int KeyUpMessage = 0x0101;
    private const long PreviousKeyState = 1L << 30;
    private const int ScanCodeWithExtendedFlag = 0x1FF;

    private enum ScanCode
    {
        A = 0x1E,
        D = 0x20,
        J = 0x24,
        K = 0x25,
        L = 0x26,
        Semicolon = 0x27,
        Space = 0x39,
        Left = 0x14B,
        Right = 0x14D,
        Down = 0x150
    }

    public static bool IsRepeat(nint keyData) => ((long)keyData & PreviousKeyState) != 0;

    public static GameCommand? GetCommand(ControlScheme scheme, nint keyData)
    {
        var key = (ScanCode)(((long)keyData >> 16) & ScanCodeWithExtendedFlag);
        return (scheme, key) switch
        {
            (ControlScheme.Arrows, ScanCode.Left) or (ControlScheme.HomeRow, ScanCode.J) => GameCommand.StartMoveLeft,
            (ControlScheme.Arrows, ScanCode.Right) or (ControlScheme.HomeRow, ScanCode.Semicolon) => GameCommand.StartMoveRight,
            (ControlScheme.Arrows, ScanCode.A) or (ControlScheme.HomeRow, ScanCode.K) => GameCommand.RotateCounterclockwise,
            (ControlScheme.Arrows, ScanCode.D) or (ControlScheme.HomeRow, ScanCode.L) => GameCommand.RotateClockwise,
            (ControlScheme.Arrows, ScanCode.Down) or (ControlScheme.HomeRow, ScanCode.Space) => GameCommand.StartDash,
            _ => null
        };
    }

    public static GameCommand? GetReleaseCommand(GameCommand command) => command switch
    {
        GameCommand.StartMoveLeft => GameCommand.StopMoveLeft,
        GameCommand.StartMoveRight => GameCommand.StopMoveRight,
        GameCommand.StartDash => GameCommand.StopDash,
        _ => null
    };
}
