using Apia.Tests.Entities;

namespace Apia.Tests.Query;

/// <summary>How many posts one author has written.</summary>
public sealed class PostCount(Guid authorId) : IProjectionQuery<int>
{
    public async Task<int> Result(IMemory memory)
        => await memory.Vault<Post>().All().CountAsync(post => post.AuthorId == authorId);
}
