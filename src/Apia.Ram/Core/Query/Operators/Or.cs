using System.Linq.Expressions;

namespace Apia;

public static partial class QueryExtensions
{
    /// <summary>Adds an OR condition on the selected field.</summary>
    public static IConditionBuilder<T> Or<T, TValue>(this Query<T> query, Expression<Func<T, TValue>> field)
        => new ConditionBuilder<T>(query, Connector.Or, ((MemberExpression)field.Body).Member.Name);
}
