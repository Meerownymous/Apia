namespace Apia;

public sealed class ConditionBuilder<T>(Query<T> query, Connector connector, string field)
    : IConditionBuilder<T>
{
    public Query<T> Complete(Func<Connector, string, ConditionNode> factory)
        => query.Append(factory(connector, field));
}
