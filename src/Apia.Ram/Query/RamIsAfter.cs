namespace Apia.Ram.Query;

/// <summary>A condition that holds when a field's value is greater than a reference value.</summary>
public sealed class RamIsAfter<T>(IsAfterNode node) : ICondition<T>
{
    private readonly IField<T> field = new RamField<T>(node.Field);

    /// <inheritdoc/>
    public bool Matches(T item) => Comparer<object?>.Default.Compare(field.Value(item), node.Value) > 0;
}
