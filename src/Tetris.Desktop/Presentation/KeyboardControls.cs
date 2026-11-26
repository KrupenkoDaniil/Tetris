using Tetris.Core.Gameplay;
using Tetris.Desktop.Application;

namespace Tetris.Desktop.Presentation;

internal static class KeyboardControls
{
    public const int KeyDownMessage = 0x0100;
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
        Left = 0x14B,
        Right = 0x14D
    }

    public static bool IsRepeat(nint keyData) => ((long)keyData & PreviousKeyState) != 0;

    public static GameCommand? GetMovement(ControlScheme scheme, nint keyData)
    {
        var key = (ScanCode)(((long)keyData >> 16) & ScanCodeWithExtendedFlag);
        return (scheme, key) switch
        {
            (ControlScheme.Arrows, ScanCode.Left) or (ControlScheme.HomeRow, ScanCode.J) => GameCommand.MoveLeft,
            (ControlScheme.Arrows, ScanCode.Right) or (ControlScheme.HomeRow, ScanCode.Semicolon) => GameCommand.MoveRight,
            (ControlScheme.Arrows, ScanCode.A) or (ControlScheme.HomeRow, ScanCode.K) => GameCommand.RotateCounterclockwise,
            (ControlScheme.Arrows, ScanCode.D) or (ControlScheme.HomeRow, ScanCode.L) => GameCommand.RotateClockwise,
            _ => null
        };
    }
}
