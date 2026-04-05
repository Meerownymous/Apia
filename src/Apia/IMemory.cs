namespace Apia;

public interface IMemory
{
    /// <summary>Many records, addressable by Guid.</summary>
    IMutableCatalog<TResult> Catalog<TResult>();

    /// <summary>A single record with no Guid — settings, config, state.</summary>
    IMutable<TResult> Mutable<TResult>();

    /// <summary>Query a projection. Register the source via IMemoryMap.</summary>
    IProjection<TResult, TQuery> Synopsis<TResult, TQuery>() where TQuery : Query<TResult>;

    /// <summary>Begin a transaction. Commit() to persist, or dispose to rollback.</summary>
    ITransaction Begin();
}
