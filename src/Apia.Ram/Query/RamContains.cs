namespace Apia.Ram.Query;

public sealed class RamContains<T>(ContainsNode node) : IRamCondition<T>
{
    private readonly RamField<T> field = new(node.Field);

    public bool Matches(T item)
    {
        if (field.Read(item) is not string s) return false;
        return s.Contains(node.Value, node.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
