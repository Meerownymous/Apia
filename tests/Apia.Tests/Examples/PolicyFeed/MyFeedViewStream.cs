using Apia.Tests.Record;

namespace Apia.Tests.Examples.PolicyFeed;

/// <summary>
/// Returns posts for the current user. When registered as a backend origin and wrapped by
/// PolicyMemory, PolicyAwareViewStream injects the UserContext into the query before calling
/// this stream — enabling the filter below to run against the raw backend efficiently.
/// When called directly (not registered), the entity-level PolicyEnforcedEntities takes over.
/// </summary>
public sealed class MyFeedViewStream(IMemory memory) : ShallowViewStream<PostRecord, MyFeedQuery>(memory)
{
    protected override async IAsyncEnumerable<PostRecord> Query(MyFeedQuery query, IMemory memory)
    {
        var count = 0;
        await foreach (var post in memory.Entities<PostRecord>().All())
        {
            if (count >= query.Limit) yield break;
            if (query.Context is null || query.Context.IsAdmin || post.AuthorId == query.Context.UserId)
            {
                yield return post;
                count++;
            }
        }
    }
}
