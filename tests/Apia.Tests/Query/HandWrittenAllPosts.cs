using Apia.Tests.Entities;

namespace Apia.Tests.Query;

/// <summary>A backend's own answer to <see cref="AllPosts"/>, standing in for a hand-written statement.</summary>
public sealed class HandWrittenAllPosts(Post post) : IAggregateOverride<AllPosts, Post>
{
    public IAsyncEnumerable<Post> Results(AllPosts query, IMemory memory)
        => new[] { post }.ToAsyncEnumerable();
}
