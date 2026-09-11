namespace Apia;

/// <summary>The vaults of a branch's memory: staged changes layered over what the stores hold.</summary>
public sealed class StagedVaults(IStores stores, IStagings stagings, IIdentities identities) : IVaults
{
    public IVault<T> Vault<T>() where T : notnull
        => new StagedVault<T>(stores.Store<T>(), stagings.Entries<T>(), new GivenIdentity<T>(identities));
}
