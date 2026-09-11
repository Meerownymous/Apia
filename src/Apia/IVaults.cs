namespace Apia;

/// <summary>The vaults a memory reads through, one per entity type.</summary>
public interface IVaults
{
    /// <summary>Read access to the entities of type T.</summary>
    IVault<T> Vault<T>() where T : notnull;
}
