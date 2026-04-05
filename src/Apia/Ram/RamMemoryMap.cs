using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>
/// Compose an in-memory IMemory. Register all stores before calling Build().
/// Synopsis sources receive IMemory as context — use it to query other entities.
/// </summary>
public sealed class RamMemoryMap : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> entities = new();
    private readonly ConcurrentDictionary<Type, object> vaults   = new();
    private readonly ConcurrentDictionary<(Type, Type), object> sources = new();

    /// <inheritdoc/>
    public void Register<TResult>(IEntities<TResult> e)
        => entities[typeof(TResult)] = e;

    /// <inheritdoc/>
    public void Register<TResult>(IVault<TResult> vault)
        => vaults[typeof(TResult)] = vault;

    /// <summary>Register a synopsis source. TContext is IMemory for Ram.</summary>
    public void Register<TResult, TQuery>(ISynopsis<TResult, TQuery, IMemory> source)
        where TQuery : Query<TResult>
        => sources[(typeof(TResult), typeof(TQuery))] = source;

    /// <inheritdoc/>
    public IMemory Build() => new RamMemory(entities, vaults, sources);
}
