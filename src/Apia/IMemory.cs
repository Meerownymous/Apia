namespace Apia;

public interface IMemory
{
    IAsyncEnumerable<T> Aggregate<T>(object query);
    Task<T> Projection<T>(object query);
    IVault<T> Vault<T>();
    IBranch Branch();
}
