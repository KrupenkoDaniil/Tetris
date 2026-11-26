namespace Tetris.Core.Records;

public sealed record RecordBook(IReadOnlyList<RecordEntry> Recent, IReadOnlyList<RecordEntry> Top)
{
    public const int RecentLimit = 10;
    public const int TopLimit = 5;

    public static RecordBook Empty { get; } = new([], []);

    public RecordBook Add(RecordEntry entry)
    {
        if (Recent.Any(existing => existing.Id == entry.Id) || Top.Any(existing => existing.Id == entry.Id))
            return this;

        var recent = Recent.Prepend(entry).Take(RecentLimit).ToArray();
        var top = Top.Append(entry)
            .OrderByDescending(record => record.Result.Score)
            .ThenBy(record => (decimal)record.Result.ActiveTime.Ticks * (record.Result.InitialLevel + 5))
            .Take(TopLimit)
            .ToArray();

        return new RecordBook(Array.AsReadOnly(recent), Array.AsReadOnly(top));
    }
}
