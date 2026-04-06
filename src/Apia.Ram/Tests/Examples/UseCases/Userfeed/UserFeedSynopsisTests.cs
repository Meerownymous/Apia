using Apia.Ram.Tests.Assert;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Ram.Tests.Examples.UseCases.Userfeed;

public sealed class UserFeedProjectionTests
{
    [Fact]
    public async Task BuildsSynopsis()
    {
        var map = new RamMemoryMap();
        map.Register(new RamEntities<PostRecord>(p => p.PostId));
        map.Register(new RamEntities<CommentRecord>(c => c.CommentId));
        map.Register(new RamEntities<UserRecord>(u => u.UserId));
        map.Register(new UserFeedSynopsis());
        var memory = map.Build();

        UserRecord user1 = new(Guid.NewGuid(), "Miro");
        UserRecord user2 = new(Guid.NewGuid(), "Ralph");
        PostRecord post = new(Guid.NewGuid(), user1.UserId, "Great Unittest discovered", LikeCount: 1, new HashSet<Guid>(), DateTime.Now );
        CommentRecord comment = new (Guid.NewGuid(), post.PostId, user2.UserId, "My cat's breath smells like cat food", DateTime.Now);
        await memory.Entities<UserRecord>().Save(user1);
        await memory.Entities<UserRecord>().Save(user2);
        await memory.Entities<PostRecord>().Save(post);
        await memory.Entities<CommentRecord>().Save(comment);

        AssertRecord.Satisfies(
            new UserFeedSynopsis()
            {
                    
            },
            await
                memory.Views<UserPostSummary, UserRecord>()
                    .Query(
                        new Query<UserRecord>()
                            .Where(user => user.UserId)
                            .Is(user1.UserId)
                    ).FirstAsync()
        );
    }
}