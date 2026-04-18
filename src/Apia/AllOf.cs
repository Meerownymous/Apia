namespace Apia;

/// <summary>A query that selects all stored entities of type <typeparamref name="T"/>.</summary>
public sealed class AllOf<T> : IQuery<AllOf<T>, T>
{
    /// <summary>Returns itself as the seed.</summary>
    public AllOf<T> Seed() => this;
}
