using System.Collections.Concurrent;
using OneOf;

namespace Apia.Ram;

public sealed class BufferedRamMutable<TResult> : IMutable<TResult>
{
    private readonly RamMutable<TResult> inner;
    private readonly ConcurrentDictionary<Type, object> buffer;
    private readonly List<Func<Task>> operations;

    internal BufferedRamMutable(
        RamMutable<TResult> inner,
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

    public Task Save(TResult record)
    {
        buffer[typeof(TResult)] = record!;
        operations.Add(() => inner.Save(record));
        return Task.CompletedTask;
    }
}