using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>
/// In-memory catalog backed by ConcurrentDictionary.
/// idOf: extracts the Guid key from a record — e.g. p => p.PostId.
/// label: optional debug label, no effect on storage.
/// </summary>
public sealed class RamEntities<TResult> : IEntities<TResult>
{
    private readonly ConcurrentDictionary<Guid, Versioned<TResult>> store = new();
    private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();
    private readonly Func<TResult, Guid> idOf;
    private readonly Func<TResult, string> label;

    public RamEntities(Func<TResult, Guid> idOf, Func<TResult, string> label)
    {
        this.idOf  = idOf;
        this.label = label;
    }

    public RamEntities(Func<TResult, Guid> idOf)
        : this(idOf, r => idOf(r).ToString())
    {
    }

    public Task<TResult> Load(Guid id)
    {
        if (!store.TryGetValue(id, out var versioned))
            throw new KeyNotFoundException($"No {typeof(TResult).Name} found with id {id}.");

        loadedVersions[id] = versioned.Version;
        return Task.FromResult(versioned.Record);
    }

    public Task Save(TResult record)
    {
        var id = idOf(record);
        store.AddOrUpdate(
            key:                id,
            addValueFactory:    _ => new Versioned<TResult>(record, 1),
            updateValueFactory: (_, existing) =>
            {
                var expected = loadedVersions.GetValueOrDefault(id, 0u);
                if (existing.Version != expected)
                    throw new ConcurrentModificationException(typeof(TResult), id);
                return new Versioned<TResult>(record, existing.Version + 1);
            });
        return Task.CompletedTask;
    }

    public Task Delete(Guid id)
    {
        store.TryRemove(id, out _);
        loadedVersions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    internal string Label(TResult record) => label(record);

    public Func<TResult, Guid> IdOf => idOf;

    public async IAsyncEnumerable<TResult> All()
    {
        foreach (var id in store.Keys)
            yield return await Task.FromResult(store[id].Record);
    }
}
