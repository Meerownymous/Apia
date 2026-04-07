namespace Apia.Ram.Query;

/// <summary>A query visitor that accumulates a condition, sort, and pagination from query nodes.</summary>
public sealed class RamQueryVisitor<T> : IQueryVisitor<T>
{
    private ICondition<T> condition = new RamAlways<T>();
    private readonly RamSort<T> sort = new();
    private int skip;
    private int take = int.MaxValue;

    /// <inheritdoc/>
    public void OnCondition(ConditionNode node)
    {
        ICondition<T> next = new RamConditionFor<T>(node);
        condition = node.Connector == Connector.Or
            ? new RamOr<T>(condition, next)
            : new RamAnd<T>(condition, next);
    }

    /// <inheritdoc/>
    public void OnGroup(GroupNode node)
    {
        ICondition<T> group = GroupCondition(node.Nodes);
        condition = node.Connector == Connector.Or
            ? new RamOr<T>(condition, group)
            : new RamAnd<T>(condition, group);
    }

    /// <inheritdoc/>
    public void OnOrder(OrderNode order) => sort.Append(order);

    /// <inheritdoc/>
    public void OnPage(int s, int t) { skip = s; take = t; }

    /// <summary>The items from source after applying condition, sort, and pagination.</summary>
    public IEnumerable<T> Items(IEnumerable<T> source)
        => sort.Sorted(source.Where(condition.Matches)).Skip(skip).Take(take);

    private static ICondition<T> GroupCondition(IReadOnlyList<FilterNode> nodes)
    {
        ICondition<T> result = new RamAlways<T>();
        foreach (var node in nodes)
            result = node switch
            {
                ConditionNode c => c.Connector == Connector.Or
                    ? new RamOr<T>(result, new RamConditionFor<T>(c))
                    : new RamAnd<T>(result, new RamConditionFor<T>(c)),
                GroupNode g => g.Connector == Connector.Or
                    ? new RamOr<T>(result, GroupCondition(g.Nodes))
                    : new RamAnd<T>(result, GroupCondition(g.Nodes)),
                _ => result
            };
        return result;
    }
}
