using System.Collections.Concurrent;
using Apia.Ram.Core;
using OneOf;

namespace Apia.Ram;

public sealed class BufferedRamVault<TResult> : IVault<TResult>
{
    private readonly RamVault<TResult> inner;
    private readonly ConcurrentDictionary<Type, object> buffer;
    private readonly List<Func<Task>> operations;

    internal BufferedRamVault(
        RamVault<TResult> inner,
        ConcurrentDictionary<Type, object> buffer,
        List<Func<Task>> operations)
    {
        this.inner      = inner;
        this.buffer     = buffer;
        this.operations = operations;
    }

    public async Task<OneOf<TResult, NotFound>> Load()
    {
        var result = buffer.TryGetValue(typeof(TResult), out var buffered)
            ? OneOf<TResult, NotFound>.FromT0((TResult)buffered)
            : await inner.Load();
        return result;
    }

    public Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
    {
        buffer[typeof(TResult)] = record!;
        operations.Add(async () =>
        {
            var result = await inner.Save(record);
            if (result.IsT1)
                throw new InvalidOperationException($"Conflict on flush: {typeof(TResult).Name} was modified by another process.");
        });
        return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT0(record));
    }
}
