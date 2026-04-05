using OneOf;

namespace Apia.File;

public sealed class BufferingVault<TResult>(FileVault<TResult> inner, List<Func<Task>> operations) : IVault<TResult>
{
    public Task<OneOf<TResult, NotFound>> Load() => inner.Load();

    public Task Save(TResult record)
    {
        operations.Add(() => inner.Save(record));
        return Task.CompletedTask;
    }
}