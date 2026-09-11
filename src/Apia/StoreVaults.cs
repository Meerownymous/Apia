namespace Apia;

/// <summary>The vaults of a memory reading straight from its stores.</summary>
public sealed class StoreVaults(IStores stores) : IVaults
{
    public IVault<T> Vault<T>() where T : notnull => new StoreVault<T>(stores.Store<T>());
}
