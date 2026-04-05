using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Examples;

public sealed class UserFeedProjectionTests
{
    [Fact]
    public async Task BuildsSynopsis()
    {
        var map = new RamMemoryMap();
        map.Register(new RamMutableCatalog<PostRecord>(p => p.PostId));
        map.Register(new RamMutableCatalog<CommentRecord>(c => c.CommentId));
        map.Register(new RamMutableCatalog<UserRecord>(u => u.UserId));
        map.Register(new UserFeedSynopsis());
        var memory = map.Build();

        UserRecord user1 = new(Guid.NewGuid(), "Miro");
        UserRecord user2 = new(Guid.NewGuid(), "Ralph");
        PostRecord post = new(Guid.NewGuid(), user1.UserId, "Great Unittest discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now );
        CommentRecord comment = new (Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now);
        await memory.Catalog<UserRecord>().Save(user1);
        await memory.Catalog<UserRecord>().Save(user2);
        await memory.Catalog<PostRecord>().Save(post);
        await memory.Catalog<CommentRecord>().Save(comment);

        // Im UseCase:
        var feed = 
            await
                memory.Synopsis<UserFeedProjection, UserFeedQuery>()
                    .Query(new UserFeedQuery(user1.UserId, Limit: 20))
                    .ToListAsync();
    }
}