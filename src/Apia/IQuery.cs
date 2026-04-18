namespace Apia;

/// <summary>A query that produces results of type <typeparamref name="TAggregated"/>.</summary>
public interface IQuery<TQuery, TAggregated>
{
    /// <summary>The seed value carried by this query.</summary>
    TQuery Seed();
}
