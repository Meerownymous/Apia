using Apia.Query;

namespace Apia;

internal sealed class IsOperator<T>(object? value) : IConditionOperator<T>
{
    public Query<T> Apply(IConditionBuilder<T> builder)
        => builder.Build(FilterOp.Equals, value, false);
}

public static partial class FilterExtensions
{
    public static Query<T> Is<T>(this IConditionBuilder<T> builder, object? value)
        => new IsOperator<T>(value).Apply(builder);
}
