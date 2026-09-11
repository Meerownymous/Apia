using System.Linq.Expressions;
using System.Text.Json;
using OneOf;
using Apia;

namespace Apia.File;

/// <summary>
/// The on-disk persistence of entities of type T, as JSON in a single file per type. A write serialises
/// the whole type into a pending file beside it and renames that over the type's file, so a write that
/// fails leaves the previously stored entities untouched.
/// </summary>
public sealed class FileEntityStore<T>(string directory, IIdentity<T> identity) : IEntityStore<T> where T : notnull
{
    private readonly string path = Path.Combine(directory, $"{typeof(T).Name}.json");
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public async Task<OneOf<Versioned<T>, NotFound>> Entity(Guid id)
        => (await Read()).TryGetValue(id, out var stored)
            ? OneOf<Versioned<T>, NotFound>.FromT0(stored)
            : new NotFound();

    public async IAsyncEnumerable<T> All()
    {
        foreach (var stored in (await Read()).Values)
            yield return stored.Entity;
    }

    public IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition) => All().Where(condition.Compile());

    public async Task Write(IReadOnlyCollection<T> saved, IReadOnlyCollection<Guid> removed)
    {
        await fileLock.WaitAsync();
        try { await WriteUnsafe(Entities(await ReadUnsafe(), saved, removed)); }
        finally { fileLock.Release(); }
    }

    private async Task<Dictionary<Guid, Versioned<T>>> Read()
    {
        await fileLock.WaitAsync();
        try { return await ReadUnsafe(); }
        finally { fileLock.Release(); }
    }

    private async Task<Dictionary<Guid, Versioned<T>>> ReadUnsafe()
    {
        if (!System.IO.File.Exists(path))
            return new Dictionary<Guid, Versioned<T>>();
        await using var stream = System.IO.File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Dictionary<Guid, Versioned<T>>>(stream, JsonOptions)
               ?? new Dictionary<Guid, Versioned<T>>();
    }

    /// <summary>The stored entities as they stand once these changes are applied to them.</summary>
    private Dictionary<Guid, Versioned<T>> Entities(
        Dictionary<Guid, Versioned<T>> stored,
        IEnumerable<T> saved,
        IEnumerable<Guid> removed)
    {
        foreach (var entity in saved)
            stored[identity.Of(entity)] = new Versioned<T>(entity, Guid.NewGuid());
        foreach (var id in removed)
            stored.Remove(id);
        return stored;
    }

    private async Task WriteUnsafe(Dictionary<Guid, Versioned<T>> stored)
    {
        Directory.CreateDirectory(directory);
        var pending = Path.Combine(directory, $"{typeof(T).Name}.{Guid.NewGuid():N}.pending");
        try
        {
            await Serialize(stored, pending);
            System.IO.File.Move(pending, path, overwrite: true);
        }
        catch
        {
            Discard(pending);
            throw;
        }
    }

    private static async Task Serialize(Dictionary<Guid, Versioned<T>> stored, string target)
    {
        await using var stream = System.IO.File.Open(target, FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream, stored, JsonOptions);
        stream.Flush(flushToDisk: true);
    }

    private static void Discard(string target)
    {
        try { System.IO.File.Delete(target); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
