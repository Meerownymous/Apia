using OneOf;

namespace Apia;

/// <summary>No overrides at all: every query answers itself. The root a composition extends with <c>With</c>.</summary>
public sealed class Overrides : IOverrides
{
    public OneOf<IAsyncEnumerable<T>, None> Results<T>(IAggregateQuery<T> query, IMemory memory) where T : notnull
        => new None();

    public OneOf<Task<T>, None> Result<T>(IProjectionQuery<T> query, IMemory memory) where T : notnull
        => new None();
}
