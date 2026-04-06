using System.Linq.Expressions;

namespace Apia.Query.Operators;

public static partial class FilterExtensions
{
    public static IConditionBuilder<T> And<T, TField>(this Query<T> query, Expression<Func<T, TField>> expr)
        => new ConditionBuilder<T>(query, Connector.And, Query<T>.FieldName(expr));

    public static IConditionBuilder<T> Or<T, TField>(this Query<T> query, Expression<Func<T, TField>> expr)
        => new ConditionBuilder<T>(query, Connector.Or, Query<T>.FieldName(expr));
}
