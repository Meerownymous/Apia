namespace Apia;

/// <summary>Base record for all query descriptors. TResult is the projected return type.</summary>
public abstract record Query<TResult> : IQuery<TResult>;
