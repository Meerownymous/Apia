namespace Apia.Ram.Query;

/// <summary>A condition that inverts the result of another condition.</summary>
public sealed class RamNegated<T>(ICondition<T> inner) : ICondition<T>
{
    /// <inheritdoc/>
    public bool Matches(T item) => !inner.Matches(item);
}
