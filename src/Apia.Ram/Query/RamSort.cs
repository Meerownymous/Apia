namespace Apia.Ram.Query;

/// <summary>A multi-level sort order over a sequence of items, built from query order nodes.</summary>
public sealed class RamSort<T> : ISort<T>
{
    private readonly List<OrderNode> orders = [];

    /// <summary>Appends an order node to this sort.</summary>
    public void Append(OrderNode order) => orders.Add(order);

    /// <inheritdoc/>
    public IEnumerable<T> Sorted(IEnumerable<T> source)
    {
        if (orders.Count == 0) return source;

        var first  = orders[0];
        var field0 = new RamField<T>(first.Field);
        var sorted = first.Descending
            ? source.OrderByDescending(item => field0.Value(item), Comparer<object?>.Default)
            : source.OrderBy(item => field0.Value(item), Comparer<object?>.Default);

        foreach (var order in orders.Skip(1))
        {
            var field = new RamField<T>(order.Field);
            sorted = order.Descending
                ? sorted.ThenByDescending(item => field.Value(item), Comparer<object?>.Default)
                : sorted.ThenBy(item => field.Value(item), Comparer<object?>.Default);
        }

        return sorted;
    }
}
