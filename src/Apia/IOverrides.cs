using OneOf;

namespace Apia;

/// <summary>The backend-specific query implementations a memory was composed with.</summary>
public interface IOverrides
{
    /// <summary>
    /// The results an override produces for the given aggregate query, or <see cref="None"/> when no
    /// override was given for it, which is the normal case.
    /// </summary>
    OneOf<IAsyncEnumerable<T>, None> Results<T>(IAggregateQuery<T> query, IMemory memory) where T : notnull;

    /// <summary>
    /// The result an override produces for the given projection query, or <see cref="None"/> when no
    /// override was given for it, which is the normal case.
    /// </summary>
    OneOf<Task<T>, None> Result<T>(IProjectionQuery<T> query, IMemory memory) where T : notnull;
}
