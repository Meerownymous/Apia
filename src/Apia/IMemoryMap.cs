namespace Apia;

public interface IMemoryMap
{
    /// <summary>Register an entity store. idOf extracts the Guid key from a record.</summary>
    void RegisterStore<T>(Func<T, Guid> idOf) where T : notnull;

    /// <summary>Register a multi-result aggregate query.</summary>
    void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull;

    /// <summary>Register a single-result projection query.</summary>
    void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull;

    IMemory Build();
}
