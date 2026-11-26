using Tetris.Core.Records;

namespace Tetris.Desktop.Storage;

internal sealed class InMemoryRecordStore : IRecordStore
{
    private RecordBook _book = RecordBook.Empty;

    public IReadOnlyList<RecordEntry> Recent => _book.Recent;
    public IReadOnlyList<RecordEntry> Top => _book.Top;

    public void Save(RecordEntry entry) => _book = _book.Add(entry);
}
