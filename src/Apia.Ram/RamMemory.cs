using System.Collections.Concurrent;
using Apia;
using Apia.Ram.Core;

namespace Apia.Ram;

public sealed class RamMemory : IMemoryTmp
{
    private readonly ConcurrentDictionary<Type, object>          entities;
    private readonly ConcurrentDictionary<Type, object>          vaults;
    private readonly ConcurrentDictionary<Type, object>          tmpSources;

    internal RamMemory(
        ConcurrentDictionary<Type, object>         entities,
        ConcurrentDictionary<Type, object>         vaults,
        ConcurrentDictionary<(Type, Type), object> sources,
        ConcurrentDictionary<Type, object>         tmpSources)
    {
        this.entities   = entities;
        this.vaults     = vaults;
        this.tmpSources = tmpSources;
    }

    public IEntitiesTmp<TResult> Entities<TResult>() where TResult : notnull => GetEntities<TResult>();

    private IEntitiesTmp<TResult> GetEntities<TResult>()
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not RamEntities<TResult> raw)
            throw new InvalidOperationException($"No IEntitiesTmp<{typeof(TResult).Name}> registered.");
        return raw.Scope();
    }

    // IVault — same return type in both interfaces; one impl satisfies both
    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault))
            throw new InvalidOperationException($"No IVault<{typeof(TResult).Name}> registered.");
        return (IVault<TResult>)vault;
    }
    
    public IViewStreamTmp<TResult, TQueryTarget> Views<TResult, TQueryTarget>() where TResult : notnull
    {
        if (!tmpSources.TryGetValue(typeof(TResult), out var source))
            throw new InvalidOperationException($"No ISynopsisStreamTmp<{typeof(TResult).Name}> registered.");
        return ((ISynopsisStreamTmp<TResult, TQueryTarget, IMemoryTmp>)source).Build(this);
    }
    
    public IViewTmp<TResult> View<TResult>() where TResult : notnull
    {
        if (!tmpSources.TryGetValue(typeof(TResult), out var source))
            throw new InvalidOperationException($"No ISynopsisTmp<{typeof(TResult).Name}> registered.");
        return ((ISynopsisTmp<TResult, IMemoryTmp>)source).Build(this);
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
