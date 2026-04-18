using Apia;
using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Examples.Userfeed;

public sealed class UserFeedProjectionTests
{
    [Fact]
    public async Task BuildsFeed_ReturnsOneItem()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        map.RegisterStore(new PostRecordId());
        map.RegisterStore(new CommentRecordId());
        map.RegisterQuery(new UserFeedProjection());
        var memory = map.Build();
        var user1  = new UserRecord("Miro");
        var user2  = new UserRecord("Ralph");
        var post   = new PostRecord(Guid.NewGuid(), user1.UserId, "Great unit test discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now);
        var branch = memory.Branch();
        await branch.Save(user1);
        await branch.Save(user2);
        await branch.Save(post);
        await branch.Save(new CommentRecord(Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now));
        await branch.Commit();

        Assert.Single(await memory.Aggregate<UserPostSummaryView>(new UserFeedQuery(user1.UserId, Limit: 20)).ToListAsync());
    }

    [Fact]
    public async Task BuildsFeed_ReturnsCorrectPostId()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        map.RegisterStore(new PostRecordId());
        map.RegisterStore(new CommentRecordId());
        map.RegisterQuery(new UserFeedProjection());
        var memory = map.Build();
        var user1  = new UserRecord("Miro");
        var user2  = new UserRecord("Ralph");
        var post   = new PostRecord(Guid.NewGuid(), user1.UserId, "Great unit test discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now);
        var branch = memory.Branch();
        await branch.Save(user1);
        await branch.Save(user2);
        await branch.Save(post);
        await branch.Save(new CommentRecord(Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now));
        await branch.Commit();

        Assert.Equal(post.PostId, (await memory.Aggregate<UserPostSummaryView>(new UserFeedQuery(user1.UserId, Limit: 20)).ToListAsync())[0].PostId);
    }

    [Fact]
    public async Task BuildsFeed_ReturnsCorrectAuthorName()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        map.RegisterStore(new PostRecordId());
        map.RegisterStore(new CommentRecordId());
        map.RegisterQuery(new UserFeedProjection());
        var memory = map.Build();
        var user1  = new UserRecord("Miro");
        var user2  = new UserRecord("Ralph");
        var post   = new PostRecord(Guid.NewGuid(), user1.UserId, "Great unit test discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now);
        var branch = memory.Branch();
        await branch.Save(user1);
        await branch.Save(user2);
        await branch.Save(post);
        await branch.Save(new CommentRecord(Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now));
        await branch.Commit();

        Assert.Equal(user1.Username, (await memory.Aggregate<UserPostSummaryView>(new UserFeedQuery(user1.UserId, Limit: 20)).ToListAsync())[0].AuthorName);
    }

    [Fact]
    public async Task BuildsFeed_ReturnsCorrectCommentCount()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        map.RegisterStore(new PostRecordId());
        map.RegisterStore(new CommentRecordId());
        map.RegisterQuery(new UserFeedProjection());
        var memory = map.Build();
        var user1  = new UserRecord("Miro");
        var user2  = new UserRecord("Ralph");
        var post   = new PostRecord(Guid.NewGuid(), user1.UserId, "Great unit test discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now);
        var branch = memory.Branch();
        await branch.Save(user1);
        await branch.Save(user2);
        await branch.Save(post);
        await branch.Save(new CommentRecord(Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now));
        await branch.Commit();

        Assert.Equal(1, (await memory.Aggregate<UserPostSummaryView>(new UserFeedQuery(user1.UserId, Limit: 20)).ToListAsync())[0].CommentCount);
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

        Assert.Empty(await memory.Aggregate<UserPostSummaryView>(new UserFeedQuery(user.UserId, Limit: 20)).ToListAsync());
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

        Assert.Empty(await memory.Aggregate<UserPostSummaryView>(new UserFeedQuery(Guid.NewGuid(), Limit: 20)).ToListAsync());
    }
}
