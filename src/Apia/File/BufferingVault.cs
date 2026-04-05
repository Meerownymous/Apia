using OneOf;

namespace Apia.File;

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