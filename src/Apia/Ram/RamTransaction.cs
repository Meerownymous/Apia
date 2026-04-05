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
    private readonly TransactionalRamMemory transactionalMemory;

    internal RamTransaction(RamMemory memory)
    {
        transactionalMemory = new TransactionalRamMemory(memory);
    }

    public IMemory Memory() => transactionalMemory;

    public async Task Commit() => await transactionalMemory.Flush();

    public ValueTask DisposeAsync()
    {
        transactionalMemory.Discard();
        return ValueTask.CompletedTask;
    }
}

internal sealed class TransactionalRamMemory : IMemory
{
    private readonly RamMemory source;
    private readonly List<Func<Task>> operations = new();
    private static readonly object DeletedMarker = new();
    private readonly ConcurrentDictionary<(Type, Guid), object> catalogBuffer = new();
    private readonly ConcurrentDictionary<Type, object> mutableBuffer = new();

    internal TransactionalRamMemory(RamMemory source) => this.source = source;

    public IMutableCatalog<TResult> Catalog<TResult>()
        => new BufferedRamCatalog<TResult>(source.RawCatalog<TResult>(), catalogBuffer, operations, DeletedMarker);

    public IMutable<TResult> Mutable<TResult>()
        => new BufferedRamMutable<TResult>(source.RawMutable<TResult>(), mutableBuffer, operations);

    public IProjection<TResult, TQuery> Synopsis<TResult, TQuery>() where TQuery : Query<TResult>
        => source.Synopsis<TResult, TQuery>();

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");

    internal async Task Flush()
    {
        foreach (var op in operations)
            await op();
    }

    internal void Discard() => operations.Clear();
}

internal sealed class BufferedRamCatalog<TResult> : IMutableCatalog<TResult>
{
    private readonly RamMutableCatalog<TResult> inner;
    private readonly ConcurrentDictionary<(Type, Guid), object> buffer;
    private readonly List<Func<Task>> operations;
    private readonly object deletedMarker;

    internal BufferedRamCatalog(
        RamMutableCatalog<TResult> inner,
        ConcurrentDictionary<(Type, Guid), object> buffer,
        List<Func<Task>> operations,
        object deletedMarker)
    {
        this.inner         = inner;
        this.buffer        = buffer;
        this.operations    = operations;
        this.deletedMarker = deletedMarker;
    }

    public async Task<TResult> Load(Guid id)
    {
        var found = buffer.TryGetValue((typeof(TResult), id), out var buffered);
        if (ReferenceEquals(buffered, deletedMarker))
            throw new KeyNotFoundException($"No {typeof(TResult).Name} found with id {id}.");
        var result = found ? (TResult)buffered! : await inner.Load(id);
        return result;
    }

    public Task Save(TResult record)
    {
        buffer[(typeof(TResult), inner.IdOf(record))] = record!;
        operations.Add(() => inner.Save(record));
        return Task.CompletedTask;
    }

    public Task Delete(Guid id)
    {
        buffer[(typeof(TResult), id)] = deletedMarker;
        operations.Add(() => inner.Delete(id));
        return Task.CompletedTask;
    }

    public Func<TResult, Guid> IdOf => inner.IdOf;

    public IAsyncEnumerable<Guid> Ids() => inner.Ids();
}
