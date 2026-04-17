using Apia;
using OneOf;

namespace Apia.File;

/// <summary>Read-only vault backed by a <see cref="FileEntityStore{T}"/>.</summary>
public sealed class FileVault<T>(FileEntityStore<T> store) : IVault<T>
{
    public Task<OneOf<T, NotFound>> Load(Guid id) => store.Get(id);
}
