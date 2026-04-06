namespace Apia.Ram.Query;

internal sealed class RamSort<T>
{
    private readonly List<OrderNode> orders = [];

    internal void Append(OrderNode order) => orders.Add(order);

    internal IEnumerable<T> Apply(IEnumerable<T> source)
    {
        if (orders.Count == 0) return source;

        var first  = orders[0];
        var field0 = new RamField<T>(first.Field);
        var sorted = first.Descending
            ? source.OrderByDescending(item => field0.Read(item), Comparer<object?>.Default)
            : source.OrderBy(item => field0.Read(item), Comparer<object?>.Default);

        foreach (var order in orders.Skip(1))
        {
            var field = new RamField<T>(order.Field);
            sorted = order.Descending
                ? sorted.ThenByDescending(item => field.Read(item), Comparer<object?>.Default)
                : sorted.ThenBy(item => field.Read(item), Comparer<object?>.Default);
        }

        return sorted;
    }
}
