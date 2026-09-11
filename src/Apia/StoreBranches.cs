namespace Apia;

/// <summary>The units of work a memory over stores hands out.</summary>
public sealed class StoreBranches(IStores stores, IIdentities identities, IOverrides overrides) : IBranches
{
    public IBranch Branch() => new StoreBranch(stores, identities, overrides, this);
}
