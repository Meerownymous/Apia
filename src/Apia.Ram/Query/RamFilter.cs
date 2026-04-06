namespace Apia.Ram.Query;

internal sealed class RamFilter<T>
{
    private Func<T, bool> predicate = _ => true;

    internal void Append(ConditionNode node)
    {
        var condition = new RamConditionFor<T>(node);
        predicate = node.Connector == Connector.Or
            ? item => predicate(item) || condition.Matches(item)
            : item => predicate(item) && condition.Matches(item);
    }

    internal void AppendGroup(GroupNode node)
    {
        var group = new RamFilter<T>();
        foreach (var child in node.Nodes)
            switch (child)
            {
                case ConditionNode c: group.Append(c);      break;
                case GroupNode     g: group.AppendGroup(g); break;
            }

        predicate = node.Connector == Connector.Or
            ? item => predicate(item) || group.Matches(item)
            : item => predicate(item) && group.Matches(item);
    }

    internal bool Matches(T item) => predicate(item);

    internal IEnumerable<T> Apply(IEnumerable<T> source) => source.Where(predicate);
}
