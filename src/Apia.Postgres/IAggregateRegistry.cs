using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>A registry of session-aware aggregate query sources for entities of type T.</summary>
public interface IAggregateRegistry<T> where T : notnull
{
    /// <summary>Registers a session-aware source for the given query type.</summary>
    void Register<TQuery>(Func<TQuery, IMemory, IQuerySession, IAsyncEnumerable<T>> source) where TQuery : notnull;

    /// <summary>All registered sources, keyed by query type.</summary>
    IReadOnlyDictionary<Type, Func<object, IMemory, IQuerySession, IAsyncEnumerable<T>>> Sources();
}
