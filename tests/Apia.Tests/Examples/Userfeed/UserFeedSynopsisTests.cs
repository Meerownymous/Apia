using Apia.Ram;
using Apia.Tests.Examples.Userfeed;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Examples;

public sealed class UserFeedProjectionTests
{
    [Fact]
    public async Task BuildsSynopsis()
    {
        var map = new RamMemoryMap();
        map.Register(new RamEntities<PostRecord>(p => p.PostId));
        map.Register(new RamEntities<CommentRecord>(c => c.CommentId));
        map.Register(new RamEntities<UserRecord>(u => u.UserId));
        var memory = map.Build();

        UserRecord user1 = new("Miro");
        UserRecord user2 = new("Ralph");
        PostRecord post = new(Guid.NewGuid(), user1.UserId, "Great Unittest discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now);
        CommentRecord comment = new (Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now);
        await memory.Entities<UserRecord>().Save(user1);
        await memory.Entities<UserRecord>().Save(user2);
        await memory.Entities<PostRecord>().Save(post);
        await memory.Entities<CommentRecord>().Save(comment);

        var feed =
            await new UserFeedViewStream(memory)
                .From(new(user1.UserId, Limit: 20))
                .ToListAsync();

        Assert.Single(feed);
        Assert.Equal(post.PostId, feed[0].PostId);
        Assert.Equal(user1.Username, feed[0].AuthorName);
        Assert.Equal(1, feed[0].CommentCount);
    }
}
