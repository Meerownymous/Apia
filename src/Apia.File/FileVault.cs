using Apia;
using OneOf;

namespace Apia.File;

/// <summary>Read-only vault backed by an IEntityStore.</summary>
public sealed class FileVault<T>(IEntityStore<T> store) : IVault<T>
{
    public Task<OneOf<T, NotFound>> Load(Guid id) => store.Get(id);
}
