namespace Apia;

public interface IMemoryMap
{
    /// <summary>Register an entity store. idOf extracts the Guid key from a record.</summary>
    void RegisterStore<T>(Func<T, Guid> idOf) where T : notnull;

    /// <summary>Register a multi-result aggregate query handler.</summary>
    void RegisterQuery<T, TQuery>(Func<TQuery, IMemory, IAsyncEnumerable<T>> handler) where T : notnull;

    /// <summary>Register a single-result projection query handler.</summary>
    void RegisterProjection<T, TQuery>(Func<TQuery, IMemory, Task<T>> handler) where T : notnull;

    IMemory Build();
}
