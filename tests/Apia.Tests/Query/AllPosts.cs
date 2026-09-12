using Apia.Tests.Entities;

namespace Apia.Tests.Query;

/// <summary>Every post there is, read through the vault and therefore through any scope in force.</summary>
public sealed class AllPosts : IAggregateQuery<Post>
{
    public IAsyncEnumerable<Post> Results(IMemory memory) => memory.Vault<Post>().All();
}
