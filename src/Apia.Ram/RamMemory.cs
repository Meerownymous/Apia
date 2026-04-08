using System.Collections.Concurrent;

namespace Apia.Ram;

/// <summary>In-memory IMemory backed by concurrent dictionaries. All reads and writes are in-process.</summary>
public sealed class RamMemory : IMemory
{
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    public RamMemory(
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.entities = entities;
        this.vaults   = vaults;
        this.sources  = sources;
    }

    public IEntities<TEntity> Entities<TEntity>() where TEntity : notnull
    {
        if (!entities.TryGetValue(typeof(TEntity), out var entry) || entry is not RamEntities<TEntity> raw)
            throw new InvalidOperationException($"No Entities<{typeof(TEntity).Name}> registered.");
        return raw.Scoped();
    }

    public IVault<TContent> Vault<TContent>() where TContent : notnull
    {
        if (!vaults.TryGetValue(typeof(TContent), out var vault))
            throw new InvalidOperationException($"No Vault<{typeof(TContent).Name}> registered.");
        return (IVault<TContent>)vault;
    }

    public IViewStream<TResult, TSeed> ViewStream<TResult, TSeed>() where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return ((ISynopsisStream<TResult, TSeed, IMemory>)source).Grow(this);
    }

    public IView<TResult, TSeed> View<TResult, TSeed>() where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return ((ISynopsis<TResult, TSeed, IMemory>)source).Build(this);
    }

    public ITransaction Begin() => new RamTransaction(entities, vaults, sources);
}
