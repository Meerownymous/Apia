namespace Apia;

/// <summary>The storage abstraction an application is written against. One container per application.</summary>
public interface IMemory
{
    /// <summary>
    /// The results of the given aggregate query. A backend override registered for the query answers
    /// it; otherwise the query answers itself, reading through this memory.
    /// </summary>
    IAsyncEnumerable<T> Aggregate<T>(IAggregateQuery<T> query) where T : notnull;

    /// <summary>
    /// The single computed result of the given projection query. A backend override registered for the
    /// query answers it; otherwise the query answers itself, reading through this memory.
    /// </summary>
    Task<T> Projection<T>(IProjectionQuery<T> query) where T : notnull;

    /// <summary>Read access to the entities of type T.</summary>
    IVault<T> Vault<T>() where T : notnull;

    /// <summary>A new unit of work over this memory.</summary>
    IBranch Branch();
}
