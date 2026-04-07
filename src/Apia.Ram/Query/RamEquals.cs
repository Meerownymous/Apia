namespace Apia.Ram.Query;

/// <summary>A condition that holds when a field's value equals a reference value.</summary>
public sealed class RamEquals<T>(EqualsNode node) : ICondition<T>
{
    private readonly IField<T> field = new RamField<T>(node.Field);

    /// <inheritdoc/>
    public bool Matches(T item) => Equals(field.Value(item), node.Value);
}
