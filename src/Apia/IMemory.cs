using OneOf;

namespace Apia;

/// <summary>The central data access facade. Composes entities, vaults, views, and transactions.</summary>
public interface IMemory
{
    /// <summary>Many records, addressable by Guid.</summary>
    IEntities<TResult> Entities<TResult>() where TResult : notnull;

    /// <summary>A single record with no Guid — settings, config, state.</summary>
    IVault<TResult> Vault<TResult>() where TResult : notnull;

    /// <summary>The stream view for TResult from TQuery, or NotFound when not registered.</summary>
    OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : Query<TResult>;

    /// <summary>The scalar view for TResult from TQuery, or NotFound when not registered.</summary>
    OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : Query<TResult>;

    /// <summary>Begin a transaction. Commit() to persist, or dispose to rollback.</summary>
    ITransaction Begin();
}
