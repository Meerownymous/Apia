using OneOf;

namespace Apia.File;

public sealed class BufferingEntities<TRecord>(FileEntities<TRecord> inner, List<Func<Task>> operations)
    : IEntities<TRecord>
{
    public Task<OneOf<TRecord, NotFound>> Load(Guid id) => inner.Load(id);

    public Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
    {
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
        operations.Add(() => inner.Delete(id));
        return Task.CompletedTask;
    }

    public Guid IdOf(TRecord record) => inner.IdOf(record);

    public IAsyncEnumerable<TRecord> All() => inner.All();
}