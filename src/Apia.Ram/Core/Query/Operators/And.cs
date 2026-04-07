using System.Linq.Expressions;

namespace Apia;

public static partial class QueryExtensions
{
    /// <summary>Adds an AND condition on the selected field.</summary>
    public static IConditionBuilder<T> And<T, TValue>(this Query<T> query, Expression<Func<T, TValue>> field)
        => new ConditionBuilder<T>(query, Connector.And, ((MemberExpression)field.Body).Member.Name);
}
