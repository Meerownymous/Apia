namespace Apia.Ram.Query;

/// <summary>A condition satisfied when either constituent condition is satisfied.</summary>
public sealed class RamOr<T>(ICondition<T> left, ICondition<T> right) : ICondition<T>
{
    /// <inheritdoc/>
    public bool Matches(T item) => left.Matches(item) || right.Matches(item);
}
