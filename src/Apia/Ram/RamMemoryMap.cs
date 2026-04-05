using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>
/// Compose an in-memory IMemory. Register all stores before calling Build().
/// Synopsis sources receive IMemory as context — use it to query other catalogs.
/// </summary>
public sealed class RamMemoryMap : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> catalogs = new();
    private readonly ConcurrentDictionary<Type, object> mutables = new();
    private readonly ConcurrentDictionary<(Type, Type), object> sources = new();

    /// <inheritdoc/>
    public void Register<TResult>(IMutableCatalog<TResult> catalog)
        => catalogs[typeof(TResult)] = catalog;

    /// <inheritdoc/>
    public void Register<TResult>(IMutable<TResult> mutable)
        => mutables[typeof(TResult)] = mutable;

    /// <summary>Register a synopsis source. TContext is IMemory for Ram.</summary>
    public void Register<TResult, TQuery>(ISynopsis<TResult, TQuery, IMemory> source)
        where TQuery : Query<TResult>
        => sources[(typeof(TResult), typeof(TQuery))] = source;

    /// <inheritdoc/>
    public IMemory Build() => new RamMemory(catalogs, mutables, sources);
}
