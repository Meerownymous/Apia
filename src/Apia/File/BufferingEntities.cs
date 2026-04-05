namespace Apia.File;

public sealed class BufferingEntities<TResult> : IEntities<TResult>
{
    private readonly FileEntities<TResult> inner;
    private readonly List<Func<Task>> operations;

    public BufferingEntities(FileEntities<TResult> inner, List<Func<Task>> operations)
    {
        this.inner      = inner;
        this.operations = operations;
    }

    public Task<TResult> Load(Guid id) => inner.Load(id);

    public Task Save(TResult record)
    {
        operations.Add(() => inner.Save(record));
        return Task.CompletedTask;
    }

    public Task Delete(Guid id)
    {
        operations.Add(() => inner.Delete(id));
        return Task.CompletedTask;
    }

    public Func<TResult, Guid> IdOf => inner.IdOf;

    public IAsyncEnumerable<TResult> All() => inner.All();
}