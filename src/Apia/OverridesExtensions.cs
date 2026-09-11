namespace Apia;

/// <summary>
/// Fluent composition of <see cref="IOverrides"/>. The constraint tying a query to the type it returns
/// lives on these generic methods, which is why overrides are composed rather than passed as a
/// constructor argument — see docs/adr/0003-memory-is-constructed-not-built.md.
/// </summary>
public static class OverridesExtensions
{
    /// <summary>These overrides, extended by a backend-specific implementation of one aggregate query.</summary>
    public static IOverrides With<TQuery, T>(this IOverrides overrides, IAggregateOverride<TQuery, T> given)
        where TQuery : IAggregateQuery<T> where T : notnull
        => new OverridesWithAggregate<TQuery, T>(overrides, given);

    /// <summary>These overrides, extended by a backend-specific implementation of one projection query.</summary>
    public static IOverrides With<TQuery, T>(this IOverrides overrides, IProjectionOverride<TQuery, T> given)
        where TQuery : IProjectionQuery<T> where T : notnull
        => new OverridesWithProjection<TQuery, T>(overrides, given);
}
