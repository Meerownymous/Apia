using Apia;
using OneOf;

namespace Apia.Ram;

/// <summary>
/// In-memory single-record store with optimistic concurrency.
/// Use for settings, config, or any singleton-style state.
/// </summary>
public sealed class RamMutable<TResult> : IMutable<TResult>
{
    private bool exists;
    private Versioned<TResult> versioned = default!;
    private uint loadedVersion;
    private readonly object syncLock = new();

    public Task<OneOf<TResult, NotFound>> Load()
    {
        lock (syncLock)
        {
            loadedVersion = exists ? versioned.Version : 0u;
            var result = exists
                ? OneOf<TResult, NotFound>.FromT0(versioned.Record)
                : OneOf<TResult, NotFound>.FromT1(new NotFound());
            return Task.FromResult(result);
        }
    }

    public Task Save(TResult record)
    {
        lock (syncLock)
        {
            if (exists && versioned.Version != loadedVersion)
                throw new ConcurrentModificationException(typeof(TResult), Guid.Empty);
            versioned = new Versioned<TResult>(record, (exists ? versioned.Version : 0u) + 1);
            exists = true;
        }
        return Task.CompletedTask;
    }
}
