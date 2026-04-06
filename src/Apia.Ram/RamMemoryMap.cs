using System.Collections.Concurrent;
using Apia;
using Apia.Ram.Core;

namespace Apia.Ram;

/// <summary>
/// Compose an in-memory IMemory / IMemoryTmp. Register all stores before calling Build().
/// Synopsis sources receive IMemory as context — use it to query other entities.
/// </summary>
public sealed class RamMemoryMap : IMemoryMapTmp
{
    private readonly ConcurrentDictionary<Type, object>          entities   = new();
    private readonly ConcurrentDictionary<Type, object>          vaults     = new();
    private readonly ConcurrentDictionary<(Type, Type), object>  sources    = new();
    private readonly ConcurrentDictionary<Type, object>          tmpSources = new();

    /// <inheritdoc/>
    public void Register<TResult>(IEntitiesTmp<TResult> e)
        => entities[typeof(TResult)] = e;

    /// <inheritdoc/>
    public void Register<TResult>(IVault<TResult> vault)
        => vaults[typeof(TResult)] = vault;

    /// <summary>Register a new-style synopsis stream source (Filter&lt;TResult&gt;-based).</summary>
    public void Register<TResult, TQueryTarget>(ISynopsisStreamTmp<TResult, TQueryTarget, IMemoryTmp> source)
        => tmpSources[typeof(TResult)] = source;

    /// <summary>Register a new-style single-result synopsis source (Filter&lt;TResult&gt;-based).</summary>
    public void Register<TResult>(ISynopsisTmp<TResult, IMemoryTmp> source)
        => tmpSources[typeof(TResult)] = source;

    /// <inheritdoc/>
    public IMemoryTmp Build() => new RamMemory(entities, vaults, sources, tmpSources);
}
