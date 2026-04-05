using System.Collections.Concurrent;

namespace Apia.Ram;

internal sealed class BufferedRamEntities<TRecord> : IEntities<TRecord>
{
    private readonly RamEntities<TRecord> inner;
    private readonly ConcurrentDictionary<(Type, Guid), object> buffer;
    private readonly List<Func<Task>> operations;
    private readonly object deletedMarker;

    internal BufferedRamEntities(
        RamEntities<TRecord> inner,
        ConcurrentDictionary<(Type, Guid), object> buffer,
        List<Func<Task>> operations,
        object deletedMarker)
    {
        this.inner         = inner;
        this.buffer        = buffer;
        this.operations    = operations;
        this.deletedMarker = deletedMarker;
    }

    public async Task<TRecord> Fetch(Guid id)
    {
        var found = buffer.TryGetValue((typeof(TRecord), id), out var buffered);
        if (ReferenceEquals(buffered, deletedMarker))
            throw new KeyNotFoundException($"No {typeof(TRecord).Name} found with id {id}.");
        var result = found ? (TRecord)buffered! : await inner.Fetch(id);
        return result;
    }

    public Task Save(TRecord record)
    {
        buffer[(typeof(TRecord), inner.IdOf(record))] = record!;
        operations.Add(() => inner.Save(record));
        return Task.CompletedTask;
    }

    public Task Delete(Guid id)
    {
        buffer[(typeof(TRecord), id)] = deletedMarker;
        operations.Add(() => inner.Delete(id));
        return Task.CompletedTask;
    }

    public Guid IdOf(TRecord record) => inner.IdOf(record);

    public IAsyncEnumerable<TRecord> All() => inner.All();
}