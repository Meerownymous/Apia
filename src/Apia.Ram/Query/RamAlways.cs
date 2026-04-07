namespace Apia.Ram.Query;

/// <summary>A condition that is always satisfied — the identity element for condition composition.</summary>
public sealed class RamAlways<T> : ICondition<T>
{
    /// <inheritdoc/>
    public bool Matches(T item) => true;
}
