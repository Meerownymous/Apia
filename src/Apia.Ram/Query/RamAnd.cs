namespace Apia.Ram.Query;

/// <summary>A condition satisfied when both constituent conditions are satisfied.</summary>
public sealed class RamAnd<T>(ICondition<T> left, ICondition<T> right) : ICondition<T>
{
    /// <inheritdoc/>
    public bool Matches(T item) => left.Matches(item) && right.Matches(item);
}
