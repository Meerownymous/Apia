using System.Collections.Concurrent;

namespace Apia.Ram;

public sealed class RamMemory : IMemory
{
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal RamMemory(
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
        return raw.Scope();
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

    public ITransaction Begin() => new RamTransaction(this);

    internal RamEntities<TResult> RawEntities<TResult>()
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not RamEntities<TResult> raw)
            throw new InvalidOperationException($"No RamEntities<{typeof(TResult).Name}> registered.");
        return raw;
    }

    internal RamVault<TResult> RawVault<TResult>()
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault) || vault is not RamVault<TResult> raw)
            throw new InvalidOperationException($"No RamVault<{typeof(TResult).Name}> registered.");
        return raw;
    }
}
