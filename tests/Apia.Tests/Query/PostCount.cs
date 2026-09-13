using Apia.Tests.Entities;

namespace Apia.Tests.Query;

/// <summary>
/// How many posts one author has written. The author it asks about is readable, so that an override
/// supplied for this query can answer what was asked rather than only that it was asked.
/// </summary>
public sealed record PostCount(Guid AuthorId) : IProjectionQuery<int>
{
    public async Task<int> Result(IMemory memory)
        => await memory.Vault<Post>().All().CountAsync(post => post.AuthorId == AuthorId);
}
