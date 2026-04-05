namespace Apia.File;

public sealed class BufferingEntities<TRecord>(FileEntities<TRecord> inner, List<Func<Task>> operations)
    : IEntities<TRecord>
{
    public Task<TRecord> Fetch(Guid id) => inner.Fetch(id);

    public Task Save(TRecord record)
    {
        operations.Add(() => inner.Save(record));
        return Task.CompletedTask;
    }

    public Task Delete(Guid id)
    {
        operations.Add(() => inner.Delete(id));
        return Task.CompletedTask;
    }

    public Guid IdOf(TRecord record) => inner.IdOf(record);

    public IAsyncEnumerable<TRecord> All() => inner.All();
}