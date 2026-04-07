namespace Apia;

public interface IMemory
{
    /// <summary>Many records, addressable by Guid.</summary>
    IEntities<TResult> Entities<TResult>() where TResult : notnull;

    /// <summary>A single record with no Guid — settings, config, state.</summary>
    IVault<TResult> Vault<TResult>() where TResult : notnull;

    /// <summary>Query a stream projection. Register the source via IMemoryMap.</summary>
    IViewStream<TResult, TSeed> ViewStream<TResult, TSeed>() where TSeed : notnull;

    /// <summary>Query a single-result projection. Register the source via IMemoryMap.</summary>
    IView<TResult, TSeed> View<TResult, TSeed>() where TSeed : notnull;

    /// <summary>Begin a transaction. Commit() to persist, or dispose to rollback.</summary>
    ITransaction Begin();
}
