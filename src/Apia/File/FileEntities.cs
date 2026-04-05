using System.Collections.Concurrent;
using System.Text.Json;
using Apia;

namespace Apia.File;

/// <summary>
/// File-backed catalog. Persists as {TypeName}.json in the given directory.
/// Thread-safe via SemaphoreSlim. Optimistic concurrency via internal versioning.
/// </summary>
public sealed class FileEntities<TRecord> : IEntities<TRecord>
{
    private readonly string path;
    private readonly Func<TRecord, Guid> idOf;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FileEntities(string directory, Func<TRecord, Guid> idOf)
    {
        Directory.CreateDirectory(directory);
        path      = Path.Combine(directory, $"{typeof(TRecord).Name}.json");
        this.idOf = idOf;
    }

    public async Task<TRecord> Fetch(Guid id)
    {
        await writeLock.WaitAsync();
        try
        {
            var store = await ReadUnsafe();
            if (!store.TryGetValue(id, out var versioned))
                throw new KeyNotFoundException($"No {typeof(TRecord).Name} found with id {id}.");
            loadedVersions[id] = versioned.Version;
            return versioned.Record;
        }
        finally { writeLock.Release(); }
    }

    public async Task Save(TRecord record)
    {
        var id = idOf(record);
        await writeLock.WaitAsync();
        try
        {
            var store = await ReadUnsafe();
            var nextVersion = store.TryGetValue(id, out var existing)
                ? CheckAndIncrement(existing.Version, id)
                : 1u;
            store[id] = new Versioned<TRecord>(record, nextVersion);
            await WriteUnsafe(store);
        }
        finally { writeLock.Release(); }
    }

    public async Task Delete(Guid id)
    {
        await writeLock.WaitAsync();
        try
        {
            var store = await ReadUnsafe();
            store.Remove(id);
            loadedVersions.TryRemove(id, out _);
            await WriteUnsafe(store);
        }
        finally { writeLock.Release(); }
    }

    public Guid IdOf(TRecord record) => idOf(record);

    public async IAsyncEnumerable<TRecord> All()
    {
        await writeLock.WaitAsync();
        List<Guid> keys;
        try
        {
            var store = await ReadUnsafe();
            keys = store.Keys.ToList();
        }
        finally { writeLock.Release(); }
        foreach (var id in keys)
            yield return await Fetch(id);
    }

    private uint CheckAndIncrement(uint currentVersion, Guid id)
    {
        var expected = loadedVersions.GetValueOrDefault(id, 0u);
        if (currentVersion != expected)
            throw new ConcurrentModificationException(typeof(TRecord), id);
        return currentVersion + 1;
    }

    private async Task<Dictionary<Guid, Versioned<TRecord>>> ReadUnsafe()
    {
        Dictionary<Guid, Versioned<TRecord>> result;
        if (!System.IO.File.Exists(path))
        {
            result = new Dictionary<Guid, Versioned<TRecord>>();
        }
        else
        {
            await using var stream = System.IO.File.OpenRead(path);
            var deserialized = await JsonSerializer.DeserializeAsync<Dictionary<Guid, Versioned<TRecord>>>(stream, JsonOptions);
            result = deserialized ?? new Dictionary<Guid, Versioned<TRecord>>();
        }
        return result;
    }

    private async Task WriteUnsafe(Dictionary<Guid, Versioned<TRecord>> store)
    {
        await using var stream = System.IO.File.Open(path, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream, store, JsonOptions);
    }
}
