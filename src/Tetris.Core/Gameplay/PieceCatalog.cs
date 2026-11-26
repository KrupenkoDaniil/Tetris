namespace Tetris.Core.Gameplay;

public interface IFigureSource
{
    int NextType();
}

public static class PieceCatalog
{
    // The order matches the numbered masks in the game specification.
    private static readonly IReadOnlyList<string>[] Masks =
    [
        Mask("#", "#", "#", "#"),
        Mask(".#.", ".#.", "###"),
        Mask("#.", "#.", "##"),
        Mask("##", "#.", "##"),
        Mask("#.", "##", "#."),
        Mask("#.", "##", "##"),
        Mask("#.", "##", ".#"),
        Mask("#.", "##", "#.", "#."),
        Mask("#", "#"),
        Mask("#.", "##"),
        Mask("##", "##"),
        Mask("#.", ".#"),
        Mask(".#.", ".##", "##."),
        Mask(".#.", "###", ".#.")
    ];

    public static int Count => Masks.Length;

    public static IReadOnlyList<string> GetMask(int type)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(type, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(type, Count);
        return Masks[type - 1];
    }

    private static IReadOnlyList<string> Mask(params string[] rows) => Array.AsReadOnly(rows);
}
