using System.Linq.Expressions;

namespace Apia;

public static partial class QueryExtensions
{
    /// <summary>Begins a filter condition on the selected field.</summary>
    public static IConditionBuilder<T> Where<T, TValue>(this Query<T> query, Expression<Func<T, TValue>> field)
        => new ConditionBuilder<T>(query, Connector.None, ((MemberExpression)field.Body).Member.Name);
}