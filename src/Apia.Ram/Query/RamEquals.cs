namespace Apia.Ram.Query;

internal sealed class RamEquals<T>(EqualsNode node) : IRamCondition<T>
{
    private readonly RamField<T> field = new(node.Field);

    public bool Matches(T item) => Equals(field.Read(item), node.Value);
}
