using OneOf;
using Apia;

namespace Apia.Ram;

/// <summary>Read-only vault backed by an IEntityStore.</summary>
public sealed class RamVault<T>(IEntityStore<T> store) : IVault<T>
{
    public Task<OneOf<T, NotFound>> Load(string id) => store.Get(id);
}
