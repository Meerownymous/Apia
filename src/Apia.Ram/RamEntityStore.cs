using System.Collections.Concurrent;
using System.Linq.Expressions;
using OneOf;
using Apia;

namespace Apia.Ram;

/// <summary>The in-memory persistence of entities of type T, keyed by id and versioned on every write.</summary>
public sealed class RamEntityStore<T>(IIdentity<T> identity) : IEntityStore<T> where T : notnull
{
    private readonly ConcurrentDictionary<Guid, Versioned<T>> entities = new();

    public Task<OneOf<Versioned<T>, NotFound>> Entity(Guid id)
        => Task.FromResult(
            entities.TryGetValue(id, out var stored)
                ? OneOf<Versioned<T>, NotFound>.FromT0(stored)
                : new NotFound());

    public IAsyncEnumerable<T> All()
        => entities.Values.Select(stored => stored.Entity).ToAsyncEnumerable();

    public IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition)
        => entities.Values.Select(stored => stored.Entity).Where(condition.Compile()).ToAsyncEnumerable();

    public Task Write(IReadOnlyCollection<T> saved, IReadOnlyCollection<Guid> removed)
    {
        foreach (var entity in IdentifiedEntities(saved))
            entities[entity.Key] = new Versioned<T>(entity.Value, Guid.NewGuid());
        foreach (var id in removed)
            entities.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    private IEnumerable<KeyValuePair<Guid, T>> IdentifiedEntities(IEnumerable<T> saved)
        => saved.Select(entity => new KeyValuePair<Guid, T>(identity.Of(entity), entity)).ToList();
}
