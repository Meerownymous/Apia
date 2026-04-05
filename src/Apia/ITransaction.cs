namespace Apia;

public interface ITransaction : IAsyncDisposable
{
    /// <summary>Transactional IMemory — pass this to use cases inside the transaction.</summary>
    IMemory Memory();

    /// <summary>Persist all changes. Without Commit(), DisposeAsync() rolls back.</summary>
    Task Commit();
}
