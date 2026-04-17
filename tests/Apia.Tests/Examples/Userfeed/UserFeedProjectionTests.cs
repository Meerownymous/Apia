using Apia;
using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Examples.Userfeed;

public sealed class UserFeedProjectionTests
{
    private static async Task<(IMemory memory, PostRecord post, UserRecord user1)> BuildFeedMemory()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        map.RegisterStore(new PostRecordId());
        map.RegisterStore(new CommentRecordId());
        map.RegisterQuery(new UserFeedProjection());
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

        return (memory, post, user1);
    }

    [Fact]
    public async Task BuildsFeed_ReturnsOneItem()
    {
        var (memory, _, user1) = await BuildFeedMemory();

        var feed = await memory.Aggregate<UserPostSummaryView>()
            .From(new UserFeedQuery(user1.UserId, Limit: 20))
            .ToListAsync();

        Assert.Single(feed);
    }

    [Fact]
    public async Task BuildsFeed_ReturnsCorrectPostId()
    {
        var (memory, post, user1) = await BuildFeedMemory();

        var feed = await memory.Aggregate<UserPostSummaryView>()
            .From(new UserFeedQuery(user1.UserId, Limit: 20))
            .ToListAsync();

        Assert.Equal(post.PostId, feed[0].PostId);
    }

    [Fact]
    public async Task BuildsFeed_ReturnsCorrectAuthorName()
    {
        var (memory, _, user1) = await BuildFeedMemory();

        var feed = await memory.Aggregate<UserPostSummaryView>()
            .From(new UserFeedQuery(user1.UserId, Limit: 20))
            .ToListAsync();

        Assert.Equal(user1.Username, feed[0].AuthorName);
    }

    [Fact]
    public async Task BuildsFeed_ReturnsCorrectCommentCount()
    {
        var (memory, _, user1) = await BuildFeedMemory();

        var feed = await memory.Aggregate<UserPostSummaryView>()
            .From(new UserFeedQuery(user1.UserId, Limit: 20))
            .ToListAsync();

        Assert.Equal(1, feed[0].CommentCount);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenUserHasNoPosts()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        map.RegisterStore(new PostRecordId());
        map.RegisterStore(new CommentRecordId());
        map.RegisterQuery(new UserFeedProjection());
        var memory = map.Build();

        var user   = new UserRecord("Miro");
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();

        var feed = await memory.Aggregate<UserPostSummaryView>()
            .From(new UserFeedQuery(user.UserId, Limit: 20))
            .ToListAsync();

        Assert.Empty(feed);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenUserNotFound()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        map.RegisterStore(new PostRecordId());
        map.RegisterStore(new CommentRecordId());
        map.RegisterQuery(new UserFeedProjection());
        var memory = map.Build();

        var feed = await memory.Aggregate<UserPostSummaryView>()
            .From(new UserFeedQuery(Guid.NewGuid(), Limit: 20))
            .ToListAsync();

        Assert.Empty(feed);
    }
}
