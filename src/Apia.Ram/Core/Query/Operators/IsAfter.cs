namespace Apia;

public sealed record IsAfterNode(
    Connector Connector,
    string    Field,
    object    Value,
    bool      Negated = false) : ConditionNode(Connector, Field, Negated);

public sealed class IsAfterOperator<T>(object value) : IConditionOperator<T>
{
    public Query<T> Apply(IConditionBuilder<T> builder)
        => builder.Complete((c, f) => new IsAfterNode(c, f, value));
}

public static partial class QueryExtensions
{
    public static Query<T> IsAfter<T>(this IConditionBuilder<T> builder, object value)
        => new IsAfterOperator<T>(value).Apply(builder);
}
