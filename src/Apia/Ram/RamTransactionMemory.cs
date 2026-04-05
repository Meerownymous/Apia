using System.Collections.Concurrent;

namespace Apia.Ram;

internal sealed class RamTransactionMemory : IMemory
{
    private readonly RamMemory source;
    private readonly ConcurrentDictionary<(Type, Guid), object> entitiesBuffer;
    private readonly ConcurrentDictionary<Type, object> vaultBuffer;
    private readonly List<Func<Task>> operations;
    private readonly object deletedMarker;

    internal RamTransactionMemory(
        RamMemory source,
        ConcurrentDictionary<(Type, Guid), object> entitiesBuffer,
        ConcurrentDictionary<Type, object> vaultBuffer,
        List<Func<Task>> operations,
        object deletedMarker)
    {
        this.source = source;
        this.entitiesBuffer = entitiesBuffer;
        this.vaultBuffer = vaultBuffer;
        this.operations = operations;
        this.deletedMarker = deletedMarker;
    }

    public IEntities<TResult> Entities<TResult>() where TResult : notnull
        => new BufferedRamEntities<TResult>(source.RawEntities<TResult>(), entitiesBuffer, operations, deletedMarker);

    public IVault<TResult> Vault<TResult>() where TResult : notnull
        => new BufferedRamVault<TResult>(source.RawVault<TResult>(), vaultBuffer, operations);

    public IViewStream<TResult, TQuery> Views<TResult, TQuery>() where TQuery : Query<TResult>
        => source.Views<TResult, TQuery>();

    public IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : Query<TResult>
        => source.View<TResult, TQuery>();

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}