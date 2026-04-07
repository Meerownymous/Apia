using System.Collections.Concurrent;

namespace Apia.Ram;

public sealed class RamTransactionMemory(
    RamMemory source,
    ConcurrentDictionary<(Type, Guid), object> entitiesBuffer,
    ConcurrentDictionary<Type, object> vaultBuffer,
    List<Func<Task>> operations,
    object deletedMarker)
    : IMemory
{
    public IEntities<TResult> Entities<TResult>() where TResult : notnull
        => new BufferedRamEntities<TResult>(source.RawEntities<TResult>().Scope(), entitiesBuffer, operations, deletedMarker);

    public IVault<TResult> Vault<TResult>() where TResult : notnull
        => new BufferedRamVault<TResult>(source.RawVault<TResult>(), vaultBuffer, operations);

    public IViewStream<TResult, TSeed> ViewStream<TResult, TSeed>() where TSeed : notnull
        => source.ViewStream<TResult, TSeed>();

    public IView<TResult, TSeed> View<TResult, TSeed>() where TSeed : notnull
        => source.View<TResult, TSeed>();

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}