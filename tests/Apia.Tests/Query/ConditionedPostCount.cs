using Apia.Tests.Entities;

namespace Apia.Tests.Query;

/// <summary>
/// A backend's own answer to <see cref="PostCount"/>: the author's posts are picked out by a condition
/// the backend evaluates, rather than every post being read and counted in process. It answers the
/// author the query names, so the same question gets the same answer either way. Every backend can run
/// it, which a hand-written statement could not.
/// </summary>
public sealed class ConditionedPostCount : IProjectionOverride<PostCount, int>
{
    public async Task<int> Result(PostCount query, IMemory memory)
        => await memory.Vault<Post>().Matching(post => post.AuthorId == query.AuthorId).CountAsync();
}
