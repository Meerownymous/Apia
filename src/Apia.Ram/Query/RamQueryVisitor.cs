namespace Apia.Ram.Query;

public sealed class RamQueryVisitor<T> : IQueryVisitor<T>
{
    private readonly RamFilter<T> filter = new();
    private readonly RamSort<T>   sort   = new();
    private int skip;
    private int take = int.MaxValue;

    public void OnCondition(ConditionNode node) => filter.Append(node);
    public void OnGroup(GroupNode node)         => filter.AppendGroup(node);
    public void OnOrder(OrderNode order)        => sort.Append(order);
    public void OnPage(int s, int t)            { skip = s; take = t; }

    public IEnumerable<T> Apply(IEnumerable<T> source)
        => sort.Apply(filter.Apply(source)).Skip(skip).Take(take);
}
