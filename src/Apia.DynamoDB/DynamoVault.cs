using OneOf;

namespace Apia.DynamoDB;

/// <summary>Read-only vault backed by a DynamoStore.</summary>
public sealed class DynamoVault<T>(IEntityStore<T> store) : IVault<T>
{
    public Task<OneOf<T, NotFound>> Load(string id) => store.Get(id);
}
