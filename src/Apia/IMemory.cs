namespace Apia;

public interface IMemory
{
    /// <summary>Many records, addressable by Guid.</summary>
    IEntities<TResult> Entities<TResult>() where TResult : notnull;

    /// <summary>A single record with no Guid — settings, config, state.</summary>
    IVault<TResult> Vault<TResult>() where TResult : notnull;

    /// <summary>Query a stream projection. Register the source via IMemoryMap.</summary>
    IViewStream<TResult, TQuery> Views<TResult, TQuery>() where TQuery : QueryRecord<TResult>;

    /// <summary>Query a single-result projection. Register the source via IMemoryMap.</summary>
    IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : QueryRecord<TResult>;

    /// <summary>Begin a transaction. Commit() to persist, or dispose to rollback.</summary>
    ITransaction Begin();
}
