using System.Collections.Concurrent;
using Apia.Ram.Core;

namespace Apia.Ram;

internal sealed class RamTransactionMemory : IMemoryTmp
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

    public IEntitiesTmp<TResult> Entities<TResult>() where TResult : notnull
        => new BufferedRamEntities<TResult>(source.RawEntities<TResult>().Scope(), entitiesBuffer, operations, deletedMarker);

    public IVault<TResult> Vault<TResult>() where TResult : notnull
        => new BufferedRamVault<TResult>(source.RawVault<TResult>(), vaultBuffer, operations);

    public IViewStreamTmp<TResult, TQueryTarget> Views<TResult, TQueryTarget>() where TResult : notnull => 
        source.Views<TResult, TQueryTarget>();

    public IViewTmp<TResult> View<TResult>() where TResult : notnull => source.View<TResult>();

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}