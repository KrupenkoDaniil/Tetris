using Tetris.Core.Records;

namespace Tetris.Desktop.Storage;

internal sealed class InMemoryRecordStore : IRecordStore
{
    private readonly List<RecordEntry> _recent = [];
    private RecordEntry[] _top = [];

    public IReadOnlyList<RecordEntry> Recent => _recent.AsReadOnly();
    public IReadOnlyList<RecordEntry> Top => Array.AsReadOnly(_top);

    public void Save(RecordEntry entry)
    {
        if (_recent.Any(existing => existing.Id == entry.Id) || _top.Any(existing => existing.Id == entry.Id))
            return;

        _recent.Insert(0, entry);
        if (_recent.Count > 10)
            _recent.RemoveAt(10);

        _top = _top
            .Append(entry)
            .OrderByDescending(record => record.Result.Score)
            .ThenBy(record => (decimal)record.Result.ActiveTime.Ticks * (record.Result.InitialLevel + 5))
            .Take(5)
            .ToArray();
    }
}
