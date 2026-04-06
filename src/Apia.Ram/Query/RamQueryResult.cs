using Tonga.Enumerable;

namespace Apia.Ram.Query;

public sealed class RamQueryResult<T>(IQuery<T> query, IEnumerable<T> source) : EnumerableEnvelope<T>(() =>
{
    var visitor = new RamQueryVisitor<T>();
    query.Accept(visitor);
    return visitor.Apply(source);
});
