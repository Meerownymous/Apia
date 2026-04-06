using System.Reflection;
using Apia.Query;

namespace Apia.Ram.Query;

public sealed class RamQueryVisitor<T> : IQueryVisitor<T>
{
    private Func<T, bool> predicate = _ => true;
    private Func<IEnumerable<T>, IOrderedEnumerable<T>> sort;
    private int skip;
    private int take = int.MaxValue;

    public void OnCondition(ConditionNode node)
    {
        var compiled = BuildPredicate(node);
        predicate = node.Connector switch
        {
            Connector.Or => Combine(predicate, compiled, or: true),
            _ => Combine(predicate, compiled, or: false),
        };
    }

    public void OnGroup(GroupNode node)
    {
        var inner = new RamQueryVisitor<T>();
        foreach (var child in node.Nodes)
            switch (child)
            {
                case ConditionNode c: inner.OnCondition(c); break;
                case GroupNode     g: inner.OnGroup(g);     break;
            }
        var groupPredicate = inner.predicate;
        predicate = node.Connector switch
        {
            Connector.Or => Combine(predicate, groupPredicate, or: true),
            _            => Combine(predicate, groupPredicate, or: false),
        };
    }

    public void OnOrder(OrderNode order)
    {
        var prop = typeof(T).GetProperty(order.Field, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{order.Field}' not found on {typeof(T).Name}.");
        Func<T, object> key = item => prop.GetValue(item);

        if (sort is null)
            sort = order.Descending
                ? src => src.OrderByDescending(key, Comparer<object?>.Default)
                : src => src.OrderBy(key, Comparer<object>.Default);
        else
        {
            var prev = sort;
            sort = order.Descending
                ? src => prev(src).ThenByDescending(key, Comparer<object>.Default)
                : src => prev(src).ThenBy(key, Comparer<object>.Default);
        }
    }

    public void OnPage(int s, int t) { skip = s; take = t; }

    public IEnumerable<T> Apply(IEnumerable<T> source)
    {
        var result = source.Where(predicate);
        if (sort is not null) result = sort(result);
        return result.Skip(skip).Take(take);
    }

    private static Func<T, bool> BuildPredicate(ConditionNode node)
    {
        var prop = typeof(T).GetProperty(node.Field, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{node.Field}' not found on {typeof(T).Name}.");

        return node.Op switch
        {
            FilterOp.Equals   => item => Equals(prop.GetValue(item), node.Value),
            FilterOp.IsAfter  => item => Compare(prop.GetValue(item), node.Value) > 0,
            FilterOp.Contains => item => Contains(prop.GetValue(item), node.Value, node.IgnoreCase),
            _ => throw new NotSupportedException($"FilterOp '{node.Op}' is not implemented in the RAM backend.")
        };
    }

    private static Func<T, bool> Combine(Func<T, bool> left, Func<T, bool> right, bool or)
        => or ? item => left(item) || right(item)
              : item => left(item) && right(item);

    private static int Compare(object a, object b)
        => Comparer<object>.Default.Compare(a, b);

    private static bool Contains(object value, object substring, bool ignoreCase)
    {
        if (value is not string s || substring is not string sub) return false;
        return s.Contains(sub, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
