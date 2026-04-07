using OneOf;

namespace Apia.Ram;

/// <summary>
/// In-memory single-record store with optimistic concurrency.
/// Use for settings, config, or any singleton-style state.
/// </summary>
public sealed class RamVault<TResult> : IVault<TResult>
{
    private bool exists;
    private Versioned<TResult> versioned = default!;
    private uint loadedVersion;
    private readonly Lock syncLock = new();

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

    public Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
    {
        lock (syncLock)
        {
            if (exists && versioned.Version != loadedVersion)
            {
                var conflict = new Conflict<TResult>(versioned.Record, record);
                return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT1(conflict));
            }
            versioned = new Versioned<TResult>(record, (exists ? versioned.Version : 0u) + 1);
            exists = true;
            return Task.FromResult(OneOf<TResult, Conflict<TResult>>.FromT0(record));
        }
    }
}
