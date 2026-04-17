using Apia;
using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Examples.Userfeed;

public sealed class UserFeedProjectionTests
{
    [Fact]
    public async Task BuildsFeed()
    {
        var map = new RamMemoryMap();
        map.RegisterStore<UserRecord>(u => u.UserId);
        map.RegisterStore<PostRecord>(p => p.PostId);
        map.RegisterStore<CommentRecord>(c => c.CommentId);
        var memory = map.Build();

        var user1   = new UserRecord("Miro");
        var user2   = new UserRecord("Ralph");
        var post    = new PostRecord(Guid.NewGuid(), user1.UserId, "Great unit test discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now);
        var comment = new CommentRecord(Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now);

        var branch = memory.Branch();
        await branch.Save(user1);
        await branch.Save(user2);
        await branch.Save(post);
        await branch.Save(comment);
        await branch.Commit();

        var feed = await new UserFeedProjection(memory)
            .From(new UserFeedQuery(user1.UserId, Limit: 20))
            .ToListAsync();

        Assert.Single(feed);
        Assert.Equal(post.PostId,     feed[0].PostId);
        Assert.Equal(user1.Username,  feed[0].AuthorName);
        Assert.Equal(1,               feed[0].CommentCount);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenUserHasNoPosts()
    {
        var map = new RamMemoryMap();
        map.RegisterStore<UserRecord>(u => u.UserId);
        map.RegisterStore<PostRecord>(p => p.PostId);
        map.RegisterStore<CommentRecord>(c => c.CommentId);
        var memory = map.Build();

        var user   = new UserRecord("Miro");
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();

        var feed = await new UserFeedProjection(memory)
            .From(new UserFeedQuery(user.UserId, Limit: 20))
            .ToListAsync();

        Assert.Empty(feed);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenUserNotFound()
    {
        var map = new RamMemoryMap();
        map.RegisterStore<UserRecord>(u => u.UserId);
        map.RegisterStore<PostRecord>(p => p.PostId);
        map.RegisterStore<CommentRecord>(c => c.CommentId);
        var memory = map.Build();

        var feed = await new UserFeedProjection(memory)
            .From(new UserFeedQuery(Guid.NewGuid(), Limit: 20))
            .ToListAsync();

        Assert.Empty(feed);
    }
}
