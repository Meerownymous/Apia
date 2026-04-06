namespace Apia;

public sealed record EqualsNode(
    Connector Connector,
    string    Field,
    object    Value,
    bool      Negated = false) : ConditionNode(Connector, Field, Negated);

public sealed class IsOperator<T>(object value) : IConditionOperator<T>
{
    public Query<T> Apply(IConditionBuilder<T> builder)
        => builder.Complete((c, f) => new EqualsNode(c, f, value));
}

public static partial class QueryExtensions
{
    public static Query<T> Is<T>(this IConditionBuilder<T> builder, object value)
        => new IsOperator<T>(value).Apply(builder);
}
