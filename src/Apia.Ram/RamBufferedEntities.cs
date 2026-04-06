using System.Collections.Concurrent;
using OneOf;

namespace Apia.Ram;

internal sealed class BufferedRamEntities<TRecord> : IEntitiesTmp<TRecord>
{
    private readonly IEntitiesTmp<TRecord> inner;
    private readonly ConcurrentDictionary<(Type, Guid), object> buffer;
    private readonly List<Func<Task>> operations;
    private readonly object deletedMarker;

    internal BufferedRamEntities(
        IEntitiesTmp<TRecord> inner,
        ConcurrentDictionary<(Type, Guid), object> buffer,
        List<Func<Task>> operations,
        object deletedMarker)
    {
        this.inner         = inner;
        this.buffer        = buffer;
        this.operations    = operations;
        this.deletedMarker = deletedMarker;
    }

    public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
    {
        var found = buffer.TryGetValue((typeof(TRecord), id), out var buffered);
        if (ReferenceEquals(buffered, deletedMarker))
            return new NotFound();
        if (found)
            return (TRecord)buffered!;
        return await inner.Load(id);
    }

    public Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
    {
        buffer[(typeof(TRecord), inner.IdOf(record))] = record!;
        operations.Add(async () =>
        {
            var result = await inner.Save(record);
            if (result.IsT1)
                throw new InvalidOperationException($"Conflict on flush: {typeof(TRecord).Name} was modified by another process.");
        });
        return Task.FromResult(OneOf<TRecord, Conflict<TRecord>>.FromT0(record));
    }

    public Task Delete(Guid id)
    {
        buffer[(typeof(TRecord), id)] = deletedMarker;
        operations.Add(() => inner.Delete(id));
        return Task.CompletedTask;
    }

    public Guid IdOf(TRecord record) => inner.IdOf(record);

    public IAsyncEnumerable<TRecord> Find(IQuery<TRecord> query) => inner.Find(query);
}