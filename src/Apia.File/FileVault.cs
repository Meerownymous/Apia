using Apia;
using OneOf;

namespace Apia.File;

/// <summary>Read-only vault backed by a <see cref="FileEntityStore{T}"/>.</summary>
internal sealed class FileVault<T>(FileEntityStore<T> store) : IVault<T>
{
    public async Task<OneOf<T, NotFound>> Load(Guid id)
    {
        var entity = await store.TryGet(id);
        return entity is null ? new NotFound() : entity;
    }
}
