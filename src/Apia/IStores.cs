namespace Apia;

/// <summary>The stores of one backend, one per entity type.</summary>
public interface IStores
{
    /// <summary>The store holding entities of type T.</summary>
    IEntityStore<T> Store<T>() where T : notnull;
}
