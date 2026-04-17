using System.Collections.Concurrent;

namespace Apia.Ram;

/// <summary>Thread-safe in-memory store for a single entity type, keyed by Guid.</summary>
internal sealed class RamEntityStore<T>(Func<T, Guid> idOf)
{
    private readonly ConcurrentDictionary<Guid, T> store = new();

    internal Func<T, Guid> IdOf => idOf;

    internal bool TryGet(Guid id, out T? entity) => store.TryGetValue(id, out entity);

    internal void Set(Guid id, T entity) => store[id] = entity;

    internal void Remove(Guid id) => store.TryRemove(id, out _);

    internal IEnumerable<T> All() => store.Values;
}
