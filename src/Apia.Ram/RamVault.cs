using OneOf;
using Apia;

namespace Apia.Ram;

/// <summary>Read-only vault backed by a <see cref="RamEntityStore{T}"/>.</summary>
internal sealed class RamVault<T>(RamEntityStore<T> store) : IVault<T>
{
    public Task<OneOf<T, NotFound>> Load(Guid id)
    {
        OneOf<T, NotFound> result = store.TryGet(id, out var entity)
            ? entity!
            : new NotFound();
        return Task.FromResult(result);
    }
}
