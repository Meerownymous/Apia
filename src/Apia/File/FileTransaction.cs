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

    public IEntities<TResult> Entities<TResult>()
        => new BufferingEntities<TResult>(source.GetFileEntities<TResult>(), operations);

    public IVault<TResult> Vault<TResult>()
        => new BufferingVault<TResult>(source.GetFileVault<TResult>(), operations);

    public IViewStream<TResult, TQuery> Views<TResult, TQuery>() where TQuery : Query<TResult>
        => source.GetSource<TResult, TQuery>().Build(source.Directory);

    public IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : Query<TResult>
    //=> source.GetSource<TResult, TQuery>().Build(source.Directory);
        => throw new NotImplementedException();
        

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}

internal sealed class BufferingEntities<TResult> : IEntities<TResult>
{
    private readonly FileEntities<TResult> inner;
    private readonly List<Func<Task>> operations;

    internal BufferingEntities(FileEntities<TResult> inner, List<Func<Task>> operations)
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

internal sealed class BufferingVault<TResult> : IVault<TResult>
{
    private readonly FileVault<TResult> inner;
    private readonly List<Func<Task>> operations;

    internal BufferingVault(FileVault<TResult> inner, List<Func<Task>> operations)
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
