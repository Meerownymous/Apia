using System.Collections.Concurrent;
using OneOf;
using Apia;

namespace Apia.Ram;

public sealed class RamScopedEntities<TResult>(
    ConcurrentDictionary<Guid, Versioned<TResult>> store,
    Func<TResult, Guid> idOf)
    : IEntities<TResult>
{
    private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();

    public Task<OneOf<TResult, NotFound>> Load(Guid id)
    {
        if (!store.TryGetValue(id, out var versioned))
            return Task.FromResult(OneOf<TResult, NotFound>.FromT1(new NotFound()));
        loadedVersions[id] = versioned.Version;
        return Task.FromResult(OneOf<TResult, NotFound>.FromT0(versioned.Record));
    }

    public Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
    {
        var id = idOf(record);
        if (store.TryGetValue(id, out var existing))
        {
            var expected = loadedVersions.GetValueOrDefault(id, 0u);
            if (existing.Version != expected)
            {
                var conflict = new Conflict<TResult>(existing.Record, record);
                return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT1(conflict));
            }
            var next = new Versioned<TResult>(record, existing.Version + 1);
            if (!store.TryUpdate(id, next, existing))
            {
                store.TryGetValue(id, out var current);
                var conflict = new Conflict<TResult>(current!.Record, record);
                return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT1(conflict));
            }
        }
        else
        {
            if (!store.TryAdd(id, new Versioned<TResult>(record, 1)))
            {
                store.TryGetValue(id, out var current);
                var conflict = new Conflict<TResult>(current!.Record, record);
                return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT1(conflict));
            }
        }
        return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT0(record));
    }

    public Task Delete(Guid id)
    {
        store.TryRemove(id, out _);
        loadedVersions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Guid IdOf(TResult record) => idOf(record);

    public async IAsyncEnumerable<TResult> All()
    {
        foreach (var kv in store)
            yield return await Task.FromResult(kv.Value.Record);
    }
}
