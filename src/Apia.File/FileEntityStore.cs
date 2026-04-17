using System.Text.Json;

namespace Apia.File;

/// <summary>File-backed entity store. Persists as {TypeName}.json in the given directory.</summary>
internal sealed class FileEntityStore<T>(string directory, Func<T, Guid> idOf)
{
    private readonly string path = Path.Combine(directory, $"{typeof(T).Name}.json");
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    internal Func<T, Guid> IdOf => idOf;

    internal async Task<T?> TryGet(Guid id)
    {
        var store = await Read();
        store.TryGetValue(id, out var entity);
        return entity;
    }

    internal async Task<IEnumerable<T>> All() => (await Read()).Values;

    internal async Task Set(Guid id, T entity)
    {
        await fileLock.WaitAsync();
        try
        {
            var store = await ReadUnsafe();
            store[id] = entity;
            await WriteUnsafe(store);
        }
        finally { fileLock.Release(); }
    }

    internal async Task Remove(Guid id)
    {
        await fileLock.WaitAsync();
        try
        {
            var store = await ReadUnsafe();
            store.Remove(id);
            await WriteUnsafe(store);
        }
        finally { fileLock.Release(); }
    }

    private async Task<Dictionary<Guid, T>> Read()
    {
        await fileLock.WaitAsync();
        try { return await ReadUnsafe(); }
        finally { fileLock.Release(); }
    }

    private async Task<Dictionary<Guid, T>> ReadUnsafe()
    {
        if (!System.IO.File.Exists(path))
            return new();
        await using var stream = System.IO.File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Dictionary<Guid, T>>(stream, JsonOptions) ?? new();
    }

    private async Task WriteUnsafe(Dictionary<Guid, T> store)
    {
        Directory.CreateDirectory(directory);
        await using var stream = System.IO.File.Open(path, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream, store, JsonOptions);
    }
}
