using System.Collections.Concurrent;
using OneOf;
using Apia;

namespace Apia.Ram;

/// <summary>Thread-safe in-memory store for a single entity type, keyed by Guid.</summary>
public sealed class RamEntityStore<T>(IIdentity<T> identity)
{
    private readonly ConcurrentDictionary<Guid, T> store = new();

    public OneOf<T, NotFound> Get(Guid id)
        => store.TryGetValue(id, out var entity)
            ? OneOf<T, NotFound>.FromT0(entity!)
            : new NotFound();

    public void Set(T entity) => store[identity.Of(entity)] = entity;

    public void Remove(Guid id) => store.TryRemove(id, out _);

    public IEnumerable<T> All() => store.Values;
}
