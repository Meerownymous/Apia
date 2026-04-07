using Tonga.Enumerable;

namespace Apia.Ram.Query;

/// <summary>An enumerable result of a query applied to an in-memory source.</summary>
public sealed class RamQueryResult<T>(IQuery<T> query, IEnumerable<T> source) : EnumerableEnvelope<T>(() =>
{
    var visitor = new RamQueryVisitor<T>();
    query.Accept(visitor);
    return visitor.Items(source);
});
