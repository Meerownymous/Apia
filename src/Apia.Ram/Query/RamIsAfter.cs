namespace Apia.Ram.Query;

internal sealed class RamIsAfter<T>(IsAfterNode node) : IRamCondition<T>
{
    private readonly RamField<T> field = new(node.Field);

    public bool Matches(T item) => Comparer<object?>.Default.Compare(field.Read(item), node.Value) > 0;
}
