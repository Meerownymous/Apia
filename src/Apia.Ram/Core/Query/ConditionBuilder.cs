using Apia.Query;

namespace Apia;

internal sealed class ConditionBuilder<T>(Query<T> query, Connector connector, string field)
    : IConditionBuilder<T>
{
    public Query<T> Build(FilterOp op, object? value, bool ignoreCase)
    {
        var node = new ConditionNode(connector, field, op, value, ignoreCase);
        query.NodeList.Add(node);
        query.LastNode = node;
        return query;
    }
}
