using System.Collections.Concurrent;

namespace Apia.Ram;

/// <summary>IMemory scoped to an active RamTransaction. Reads see buffered writes; commits flush to backing stores.</summary>
public sealed class RamTransactionMemory(
    ConcurrentDictionary<Type, object> entities,
    ConcurrentDictionary<Type, object> vaults,
    ConcurrentDictionary<(Type, Type), object> sources,
    ConcurrentDictionary<(Type, Guid), object> entitiesBuffer,
    ConcurrentDictionary<Type, object> vaultBuffer,
    List<Func<Task>> operations,
    object deletedMarker)
    : IMemory
{
    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not RamEntities<TResult> raw)
            throw new InvalidOperationException($"No Entities<{typeof(TResult).Name}> registered.");
        return new BufferedRamEntities<TResult>(raw.Scoped(), entitiesBuffer, operations, deletedMarker);
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        if (!vaults.TryGetValue(typeof(TResult), out var vault))
            throw new InvalidOperationException($"No Vault<{typeof(TResult).Name}> registered.");
        return new BufferedRamVault<TResult>((IVault<TResult>)vault, vaultBuffer, operations);
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

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}
