using System.Collections.Concurrent;
using Apia;

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

    public IEntities<TResult> Entities<TResult>()
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not RamEntities<TResult> raw)
            throw new InvalidOperationException($"No IEntities<{typeof(TResult).Name}> registered.");
        return raw.Scope();
    }

    public IVault<TResult> Vault<TResult>()
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault))
            throw new InvalidOperationException($"No IVault<{typeof(TResult).Name}> registered.");
        return (IVault<TResult>)vault;
    }

    public IViewStream<TResult, TQuery> Views<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsisStream<TResult, TQuery, IMemory>)source).Build(this);
    }

    public IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsis<TResult, TQuery, IMemory>)source).Build(this);
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
