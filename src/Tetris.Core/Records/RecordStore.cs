using Tetris.Core.Gameplay;

namespace Tetris.Core.Records;

public sealed record RecordEntry(Guid Id, DateTimeOffset Date, GameResult Result);

public interface IRecordStore
{
    IReadOnlyList<RecordEntry> Recent { get; }

    IReadOnlyList<RecordEntry> Top { get; }

    void Save(RecordEntry entry);
}
