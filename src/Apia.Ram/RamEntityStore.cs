using System.Collections.Concurrent;
using OneOf;
using Apia;

namespace Apia.Ram;

/// <summary>Thread-safe in-memory store for entities of type T, keyed by Guid.</summary>
public sealed class RamEntityStore<T>(IIdentity<T> identity) : IEntityStore<T>
{
    private readonly ConcurrentDictionary<Guid, T> store = new();

    public Task<OneOf<T, NotFound>> Get(Guid id)
        => Task.FromResult(
            store.TryGetValue(id, out var entity)
                ? OneOf<T, NotFound>.FromT0(entity!)
                : new NotFound());

    public async IAsyncEnumerable<T> All()
    {
        foreach (var entity in store.Values)
            yield return entity;
    }

    public Task Set(T entity)
    {
        store[identity.Of(entity)] = entity;
        return Task.CompletedTask;
    }

    public Task Remove(Guid id)
    {
        store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
