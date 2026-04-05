namespace Apia;

/// <summary>
/// Base type for all query descriptors.
/// TResult is the projected return type — never instantiated directly.
/// </summary>
public abstract record Query<TResult>;
