using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>A registry of session-aware aggregate query sources for entities of type T.</summary>
public interface IAggregateRegistry<T>
{
    /// <summary>Registers a session-aware source for the given query type.</summary>
    void Register<TQuery>(Func<TQuery, IMemory, IDocumentSession, IAsyncEnumerable<T>> source);

    /// <summary>All registered sources, keyed by query type.</summary>
    IReadOnlyDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> Sources();
}
