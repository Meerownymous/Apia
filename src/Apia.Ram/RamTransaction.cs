using System.Collections.Concurrent;

namespace Apia.Ram;

/// <summary>
/// Buffered transaction for RamMemory.
/// Load() reads from buffer first, then falls back to the real store.
/// Save()/Delete() write to the buffer only — visible within the same transaction.
/// Commit() flushes buffer to the real stores with optimistic concurrency check.
/// DisposeAsync() without Commit() discards the buffer — rollback.
/// </summary>
public sealed class RamTransaction : ITransaction
{
    private static readonly object DeletedMarker = new();
    private readonly List<Func<Task>> operations = new();
    private readonly ConcurrentDictionary<(Type, Guid), object> entitiesBuffer = new();
    private readonly ConcurrentDictionary<Type, object> vaultBuffer = new();
    private readonly RamTransactionMemory transactionMemory;

    public RamTransaction(
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        transactionMemory = new RamTransactionMemory(
            entities, vaults, sources,
            entitiesBuffer, vaultBuffer, operations, DeletedMarker);
    }

    public IMemory Memory() => transactionMemory;

    public async Task Commit()
    {
        foreach (var op in operations)
            await op();
    }

    public ValueTask DisposeAsync()
    {
        operations.Clear();
        return ValueTask.CompletedTask;
    }
}
