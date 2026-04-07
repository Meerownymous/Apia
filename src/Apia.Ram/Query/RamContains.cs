namespace Apia.Ram.Query;

/// <summary>A condition that holds when a string field contains a given substring.</summary>
public sealed class RamContains<T>(ContainsNode node) : ICondition<T>
{
    private readonly IField<T> field = new RamField<T>(node.Field);

    /// <inheritdoc/>
    public bool Matches(T item)
    {
        if (field.Value(item) is not string s) return false;
        return s.Contains(node.Value, node.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
