namespace Apia;

/// <summary>A query that selects all stored entities of type T.</summary>
public sealed class AllOf<T> : IAllOf<T>
{
    public Type EntityType() => typeof(T);
}
