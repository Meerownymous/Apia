using System.Collections.Concurrent;
using OneOf;

namespace Apia.Ram;

public sealed class BufferedRamEntities<TRecord>(
    IEntities<TRecord> inner,
    ConcurrentDictionary<(Type, Guid), object> buffer,
    List<Func<Task>> operations,
    object deletedMarker)
    : IEntities<TRecord>
{
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

    public IAsyncEnumerable<TRecord> All() => inner.All();
}