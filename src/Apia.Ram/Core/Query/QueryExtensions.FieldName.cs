using System.Linq.Expressions;

namespace Apia;

public static partial class QueryExtensions
{
    internal static string FieldName<T, TField>(Expression<Func<T, TField>> expr)
        => ((MemberExpression)expr.Body).Member.Name;
}
