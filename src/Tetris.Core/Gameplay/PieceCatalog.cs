namespace Tetris.Core.Gameplay;

public interface IFigureSource
{
    int NextType();

    int NextTypeAfter(int previousType) => NextType();

    bool NextIsMirrored(int type) => false;
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

    private static readonly Dictionary<int, IReadOnlyList<string>> MirroredMasks =
        new[] { 3, 6, 7, 8, 13 }.ToDictionary(type => type, type => Reflect(Masks[type - 1]));

    public static int Count => Masks.Length;

    public static bool HasMirroredVariant(int type) => MirroredMasks.ContainsKey(type);

    public static IReadOnlyList<string> GetMask(int type, bool mirrored = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(type, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(type, Count);
        return mirrored && MirroredMasks.TryGetValue(type, out var reflected)
            ? reflected
            : Masks[type - 1];
    }

    private static IReadOnlyList<string> Mask(params string[] rows) => Array.AsReadOnly(rows);

    private static IReadOnlyList<string> Reflect(IReadOnlyList<string> rows) =>
        Array.AsReadOnly(rows.Select(row => new string(row.Reverse().ToArray())).ToArray());
}
