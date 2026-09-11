using OneOf;

namespace Apia;

/// <summary>Overrides extended by a backend-specific implementation of one aggregate query.</summary>
public sealed class OverridesWithAggregate<TQuery, TResult>(
    IOverrides inner,
    IAggregateOverride<TQuery, TResult> given)
    : IOverrides where TQuery : IAggregateQuery<TResult> where TResult : notnull
{
    public OneOf<IAsyncEnumerable<T>, None> Results<T>(IAggregateQuery<T> query, IMemory memory) where T : notnull
        => query is TQuery asked && given.Results(asked, memory) is IAsyncEnumerable<T> results
            ? OneOf<IAsyncEnumerable<T>, None>.FromT0(results)
            : inner.Results(query, memory);

    public OneOf<Task<T>, None> Result<T>(IProjectionQuery<T> query, IMemory memory) where T : notnull
        => inner.Result(query, memory);
}
