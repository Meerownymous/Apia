using OneOf;

namespace Apia;

/// <summary>Overrides extended by a backend-specific implementation of one projection query.</summary>
public sealed class OverridesWithProjection<TQuery, TResult>(
    IOverrides inner,
    IProjectionOverride<TQuery, TResult> given)
    : IOverrides where TQuery : IProjectionQuery<TResult> where TResult : notnull
{
    public OneOf<IAsyncEnumerable<T>, None> Results<T>(IAggregateQuery<T> query, IMemory memory) where T : notnull
        => inner.Results(query, memory);

    public OneOf<Task<T>, None> Result<T>(IProjectionQuery<T> query, IMemory memory) where T : notnull
        => query is TQuery asked && given.Result(asked, memory) is Task<T> result
            ? OneOf<Task<T>, None>.FromT0(result)
            : inner.Result(query, memory);
}
