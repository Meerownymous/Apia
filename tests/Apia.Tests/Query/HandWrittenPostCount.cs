namespace Apia.Tests.Query;

/// <summary>A backend's own answer to <see cref="PostCount"/>, standing in for a counting statement.</summary>
public sealed class HandWrittenPostCount(int count) : IProjectionOverride<PostCount, int>
{
    public Task<int> Result(PostCount query, IMemory memory) => Task.FromResult(count);
}
