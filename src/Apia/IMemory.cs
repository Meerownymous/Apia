namespace Apia;

public interface IMemory
{
    IAsyncEnumerable<T> Aggregate<T>(object query) where T : notnull;
    Task<T> Projection<T>(object query) where T : notnull;
    IVault<T> Vault<T>() where T : notnull;
    IBranch Branch();
}
