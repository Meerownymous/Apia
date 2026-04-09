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

    public OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT0(
            ((IViewStreamOrigin<TResult, TQuery, IMemory>)source).From(this));
    }

    public OneOf.OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT0(
            ((IViewOrigin<TResult, TQuery, IMemory>)source).Assemble(this));
    }

    public ITransaction Begin() => new RamTransaction(entities, vaults, sources);
}
