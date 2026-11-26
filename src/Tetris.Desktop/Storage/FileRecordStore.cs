using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tetris.Core.Records;

namespace Tetris.Desktop.Storage;

internal sealed class FileRecordStore : IRecordStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(2);

    private readonly string _directory;
    private readonly string _recentPath;
    private readonly string _topPath;
    private readonly string _pendingPath;
    private readonly string _mutexName;
    private RecordBook _book = RecordBook.Empty;

    public FileRecordStore() : this(FindRecordsDirectory(AppContext.BaseDirectory))
    {
    }

    public FileRecordStore(string directory)
    {
        _directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        _recentPath = Path.Combine(_directory, "recent.json");
        _topPath = Path.Combine(_directory, "top.json");
        _pendingPath = Path.Combine(_directory, "pending.json");

        var directoryHash = SHA256.HashData(Encoding.UTF8.GetBytes(_directory.ToUpperInvariant()));
        _mutexName = @"Local\TetrisCourseworkRecords-" + Convert.ToHexString(directoryHash);
    }

    public IReadOnlyList<RecordEntry> Recent => _book.Recent;
    public IReadOnlyList<RecordEntry> Top => _book.Top;

    public void Reload()
    {
        using var mutex = new Mutex(false, _mutexName);
        WaitForAccess(mutex);
        try
        {
            _book = ReadCurrent();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public void Save(RecordEntry entry)
    {
        Validate([entry], 1, _pendingPath);
        using var mutex = new Mutex(false, _mutexName);
        WaitForAccess(mutex);
        try
        {
            var current = ReadCurrent();
            var updated = current.Add(entry);
            if (!ReferenceEquals(current, updated))
                Persist(updated);

            _book = updated;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static string FindRecordsDirectory(string applicationDirectory)
    {
        for (DirectoryInfo? directory = new(applicationDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Tetris.slnx")))
                return Path.Combine(directory.FullName, "db");
        }

        return Path.Combine(applicationDirectory, "db");
    }

    private static void WaitForAccess(Mutex mutex)
    {
        try
        {
            if (!mutex.WaitOne(LockTimeout))
                throw new IOException("Таблица рекордов занята другим экземпляром игры.");
        }
        catch (AbandonedMutexException)
        {
            // The caller owns the mutex and can recover the interrupted write.
        }
    }

    private RecordBook ReadCurrent()
    {
        if (ReadOptional<RecordBook>(_pendingPath) is { } pending)
        {
            Validate(pending.Recent, RecordBook.RecentLimit, _pendingPath);
            Validate(pending.Top, RecordBook.TopLimit, _pendingPath);
            CompleteWrite(pending);
        }

        var recent = ReadOptional<RecordEntry[]>(_recentPath) ?? [];
        var top = ReadOptional<RecordEntry[]>(_topPath) ?? [];
        Validate(recent, RecordBook.RecentLimit, _recentPath);
        Validate(top, RecordBook.TopLimit, _topPath);
        return new RecordBook(Array.AsReadOnly(recent), Array.AsReadOnly(top));
    }

    private void Persist(RecordBook book)
    {
        Directory.CreateDirectory(_directory);
        WriteJson(_pendingPath, book);
        CompleteWrite(book);
    }

    private void CompleteWrite(RecordBook book)
    {
        WriteJson(_recentPath, book.Recent);
        WriteJson(_topPath, book.Top);
        File.Delete(_pendingPath);
    }

    private static T? ReadOptional<T>(string path) where T : class
    {
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, JsonOptions)
                ?? throw new IOException($"Пустой JSON в файле рекордов {Path.GetFileName(path)}.");
        }
        catch (IOException exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (JsonException exception)
        {
            throw new IOException($"Некорректный JSON в файле рекордов {Path.GetFileName(path)}.", exception);
        }
    }

    private static void Validate(IReadOnlyList<RecordEntry>? records, int limit, string path)
    {
        if (records is null || records.Count > limit)
            throw new IOException($"Некорректный список в файле рекордов {Path.GetFileName(path)}.");

        var ids = new HashSet<Guid>();
        foreach (var record in records)
        {
            if (record is null || record.Id == Guid.Empty || record.Date == default ||
                !ids.Add(record.Id) || record.Result is null ||
                record.Result.InitialLevel is < 1 or > 5 || record.Result.Score < 0 ||
                record.Result.ActiveTime < TimeSpan.Zero)
                throw new IOException($"Некорректная запись в файле рекордов {Path.GetFileName(path)}.");
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        var temporary = path + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
