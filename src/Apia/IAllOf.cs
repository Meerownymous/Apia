namespace Apia;

/// <summary>A query that selects all entities of type T.</summary>
public interface IAllOf<T>
{
    /// <summary>The entity type this query targets.</summary>
    Type EntityType();
}
