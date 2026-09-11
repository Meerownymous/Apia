using Apia;
using OneOf;

namespace Apia.File;

/// <summary>Read-only vault backed by an IEntityStore.</summary>
public sealed class FileVault<T>(IEntityStore<T> store) : IVault<T> where T : notnull
{
    public Task<OneOf<T, NotFound>> Load(Guid id) => store.Get(id);
}
