using System.Text.Json;
using OneOf;
using Apia;

namespace Apia.File;

/// <summary>File-backed entity store. Persists entities as JSON in a single file per type.</summary>
public sealed class FileEntityStore<T>(string directory, IIdentity<T> identity) : IEntityStore<T>
{
    private readonly string path = Path.Combine(directory, $"{typeof(T).Name}.json");
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public async Task<OneOf<T, NotFound>> Get(Guid id)
    {
        var store = await Read();
        return store.TryGetValue(id, out var entity)
            ? OneOf<T, NotFound>.FromT0(entity!)
            : new NotFound();
    }

    public async IAsyncEnumerable<T> All()
    {
        foreach (var entity in (await Read()).Values)
            yield return entity;
    }

    public async Task Set(T entity)
    {
        var id = identity.Of(entity);
        await fileLock.WaitAsync();
        try
        {
            var store = await ReadUnsafe();
            store[id] = entity;
            await WriteUnsafe(store);
        }
        finally { fileLock.Release(); }
    }

    public async Task Remove(Guid id)
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
