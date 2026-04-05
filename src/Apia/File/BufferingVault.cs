using OneOf;

namespace Apia.File;

public sealed class BufferingVault<TResult>(FileVault<TResult> inner, List<Func<Task>> operations) : IVault<TResult>
{
    public Task<OneOf<TResult, NotFound>> Load() => inner.Load();

    public Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
    {
        operations.Add(async () =>
        {
            var result = await inner.Save(record);
            if (result.IsT1)
                throw new InvalidOperationException($"Conflict on flush: {typeof(TResult).Name} was modified by another process.");
        });
        return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT0(record));
    }
}