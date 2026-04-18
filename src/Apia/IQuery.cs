namespace Apia;

/// <summary>A query that carries a typed seed value for backend processing.</summary>
public interface IQuery<TSeed>
{
    /// <summary>The seed value this query carries.</summary>
    TSeed Seed();
}
