namespace Apia;

public sealed class Query<T> : IQuery<T>
{
    private readonly IReadOnlyList<FilterNode> nodes;
    private readonly IReadOnlyList<OrderNode>  orders;
    private readonly int skip;
    private readonly int take;

    public Query()
        : this([], [], 0, int.MaxValue) { }

    private Query(
        IReadOnlyList<FilterNode> nodes,
        IReadOnlyList<OrderNode>  orders,
        int skip,
        int take)
    {
        this.nodes  = nodes;
        this.orders = orders;
        this.skip   = skip;
        this.take   = take;
    }

    public Query<T> Append(FilterNode node)
        => new([..nodes, node], orders, skip, take);

    public Query<T> AppendOrder(OrderNode order)
        => new(nodes, [..orders, order], skip, take);

    public Query<T> WithSkip(int n) => new(nodes, orders, n, take);
    public Query<T> WithTake(int n) => new(nodes, orders, skip, n);

    public void Accept(IQueryVisitor<T> visitor)
    {
        foreach (var node in nodes)
            switch (node)
            {
                case ConditionNode c: visitor.OnCondition(c); break;
                case GroupNode     g: visitor.OnGroup(g);     break;
            }
        foreach (var o in orders)
            visitor.OnOrder(o);
        if (take != int.MaxValue || skip != 0)
            visitor.OnPage(skip, take);
    }
}
