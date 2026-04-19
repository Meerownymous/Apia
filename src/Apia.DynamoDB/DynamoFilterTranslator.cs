using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Amazon.DynamoDBv2.Model;

namespace Apia.DynamoDB;

/// <summary>
/// Translates a LINQ expression tree to a DynamoDB FilterExpression string,
/// with accompanying ExpressionAttributeNames and ExpressionAttributeValues.
/// Supports: ==, !=, &lt;, &lt;=, &gt;, &gt;=, &amp;&amp;, ||, !
/// Unsupported nodes throw NotSupportedException — callers should fall back to in-process filtering.
/// </summary>
internal sealed class DynamoFilterTranslator : ExpressionVisitor
{
    private readonly Dictionary<string, string>         names  = new();
    private readonly Dictionary<string, AttributeValue> values = new();
    private readonly StringBuilder                      filter = new();
    private int counter;

    internal (string filter, Dictionary<string, string> names, Dictionary<string, AttributeValue> values)
        Translate(Expression body)
    {
        Visit(body);
        return (filter.ToString(), names, values);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        filter.Append('(');
        Visit(node.Left);
        filter.Append(node.NodeType switch
        {
            ExpressionType.Equal              => " = ",
            ExpressionType.NotEqual           => " <> ",
            ExpressionType.GreaterThan        => " > ",
            ExpressionType.LessThan           => " < ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThanOrEqual    => " <= ",
            ExpressionType.AndAlso            => " AND ",
            ExpressionType.OrElse             => " OR ",
            _ => throw new NotSupportedException($"Binary operator {node.NodeType} is not supported in DynamoDB filter translation.")
        });
        Visit(node.Right);
        filter.Append(')');
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression)
        {
            var key = $"#n{counter++}";
            names[key] = node.Member.Name;
            filter.Append(key);
        }
        else
        {
            AppendValue(Expression.Lambda(node).Compile().DynamicInvoke());
        }
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        AppendValue(node.Value);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not)
        {
            filter.Append("NOT (");
            Visit(node.Operand);
            filter.Append(')');
            return node;
        }
        return base.VisitUnary(node);
    }

    private void AppendValue(object? value)
    {
        var key = $":v{counter++}";
        values[key] = ToAttribute(value);
        filter.Append(key);
    }

    private static AttributeValue ToAttribute(object? v) => v switch
    {
        string s  => new AttributeValue { S = s },
        Guid g    => new AttributeValue { S = g.ToString() },
        bool b    => new AttributeValue { BOOL = b },
        int i     => new AttributeValue { N = i.ToString() },
        long l    => new AttributeValue { N = l.ToString() },
        double d  => new AttributeValue { N = d.ToString(CultureInfo.InvariantCulture) },
        float f   => new AttributeValue { N = f.ToString(CultureInfo.InvariantCulture) },
        decimal m => new AttributeValue { N = m.ToString(CultureInfo.InvariantCulture) },
        null      => new AttributeValue { NULL = true },
        _         => new AttributeValue { S = System.Text.Json.JsonSerializer.Serialize(v) }
    };
}
