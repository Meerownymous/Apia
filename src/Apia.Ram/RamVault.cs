using OneOf;
using Apia;

namespace Apia.Ram;

/// <summary>Read-only vault backed by a <see cref="RamEntityStore{T}"/>.</summary>
public sealed class RamVault<T>(RamEntityStore<T> store) : IVault<T>
{
    public Task<OneOf<T, NotFound>> Load(Guid id)
        => Task.FromResult(store.Get(id));
}
