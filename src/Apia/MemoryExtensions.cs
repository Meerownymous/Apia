namespace Apia;

/// <summary>
/// Throwing accessors over IMemory for call sites that treat a missing
/// registration as a programming error.
/// </summary>
public static class MemoryExtensions
{
    /// <summary>The registered view stream. Throws when not registered.</summary>
    public static IViewStream<TResult, TQuery> ViewStream<TResult, TQuery>(this IMemory memory)
        where TQuery : Query<TResult>
        => memory.TryViewStream<TResult, TQuery>().Match(
            stream => stream,
            _ => throw new InvalidOperationException(
                $"No IViewStreamOrigin<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered."));

    /// <summary>The registered scalar view. Throws when not registered.</summary>
    public static IView<TResult, TQuery> View<TResult, TQuery>(this IMemory memory)
        where TQuery : Query<TResult>
        => memory.TryView<TResult, TQuery>().Match(
            view => view,
            _ => throw new InvalidOperationException(
                $"No IViewOrigin<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered."));
}
