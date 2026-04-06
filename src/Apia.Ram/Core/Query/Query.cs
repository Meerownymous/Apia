using System.Linq.Expressions;
using Apia.Query;

namespace Apia;

public sealed class Query<T> : IQuery<T>
{
    internal readonly List<FilterNode> NodeList  = [];
    internal readonly List<OrderNode>  OrderList = [];
    internal int  Skip = 0;
    internal int  Take = int.MaxValue;
    internal ConditionNode? LastNode;

    public void Accept(IQueryVisitor<T> visitor)
    {
        foreach (var node in NodeList)
            switch (node)
            {
                case ConditionNode c: visitor.OnCondition(c); break;
                case GroupNode     g: visitor.OnGroup(g);     break;
            }
        foreach (var o in OrderList)
            visitor.OnOrder(o);
        if (Take != int.MaxValue || Skip != 0)
            visitor.OnPage(Skip, Take);
    }

    internal static string FieldName<TField>(Expression<Func<T, TField>> expr)
        => ((MemberExpression)expr.Body).Member.Name;
}
