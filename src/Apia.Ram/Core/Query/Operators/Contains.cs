namespace Apia;

public sealed record ContainsNode(
    Connector Connector,
    string    Field,
    string    Value,
    bool      IgnoreCase = false,
    bool      Negated    = false) : ConditionNode(Connector, Field, Negated);

public sealed class ContainsOperator<T>(string value) : IConditionOperator<T>
{
    public Query<T> Apply(IConditionBuilder<T> builder)
        => builder.Complete((c, f) => new ContainsNode(c, f, value));
}

public static partial class QueryExtensions
{
    public static Query<T> Contains<T>(this IConditionBuilder<T> builder, string value)
        => new ContainsOperator<T>(value).Apply(builder);
}
