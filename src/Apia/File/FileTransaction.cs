using Apia;
using OneOf;

namespace Apia.File;

public sealed class FileTransaction : ITransaction
{
    private readonly List<Func<Task>> operations = new();
    private volatile bool committed;
    private readonly BufferingMemory bufferingMemory;

    internal FileTransaction(FileMemory source)
    {
        bufferingMemory = new BufferingMemory(source, operations);
    }

    public IMemory Memory() => bufferingMemory;

    public async Task Commit()
    {
        foreach (var op in operations)
            await op();
        committed = true;
    }

    public ValueTask DisposeAsync()
    {
        if (!committed)
            operations.Clear();
        return ValueTask.CompletedTask;
    }
}

internal sealed class BufferingMemory : IMemory
{
    private readonly FileMemory source;
    private readonly List<Func<Task>> operations;

    internal BufferingMemory(FileMemory source, List<Func<Task>> operations)
    {
        this.source     = source;
        this.operations = operations;
    }

    public IMutableCatalog<TResult> Catalog<TResult>()
        => new BufferingMutableCatalog<TResult>(source.GetFileMutableCatalog<TResult>(), operations);

    public IMutable<TResult> Mutable<TResult>()
        => new BufferingMutable<TResult>(source.GetFileMutable<TResult>(), operations);

    public IProjection<TResult, TQuery> Synopsis<TResult, TQuery>() where TQuery : Query<TResult>
        => source.GetSource<TResult, TQuery>().Build(source.Directory);

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}

internal sealed class BufferingMutableCatalog<TResult> : IMutableCatalog<TResult>
{
    private readonly FileMutableCatalog<TResult> inner;
    private readonly List<Func<Task>> operations;

    internal BufferingMutableCatalog(FileMutableCatalog<TResult> inner, List<Func<Task>> operations)
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

    public IAsyncEnumerable<Guid> Ids() => inner.Ids();
}

internal sealed class BufferingMutable<TResult> : IMutable<TResult>
{
    private readonly FileMutable<TResult> inner;
    private readonly List<Func<Task>> operations;

    internal BufferingMutable(FileMutable<TResult> inner, List<Func<Task>> operations)
    {
        this.inner      = inner;
        this.operations = operations;
    }

    public Task<OneOf<TResult, NotFound>> Load() => inner.Load();

    public Task Save(TResult record)
    {
        operations.Add(() => inner.Save(record));
        return Task.CompletedTask;
    }
}
