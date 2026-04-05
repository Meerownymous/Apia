using Apia.Ram;
using Apia.Tests.Record;
using Tonga.Enumerable;

namespace Apia.Tests.Examples;

public sealed class UserFeedSynopsis : RamSynopsis<UserFeedProjection, UserFeedQuery>
{
    protected override async IAsyncEnumerable<UserFeedProjection> Query(IMemory memory, UserFeedQuery query)
    {
        var posts    = memory.Catalog<PostRecord>();
        var comments = memory.Catalog<CommentRecord>();
        var users    = memory.Catalog<UserRecord>();

        var userPosts = new List<PostRecord>();
        await foreach (var post in posts.All())
        {
            if (post.AuthorId == query.UserId)
                userPosts.Add(post);
        }

        var commentCounts = new Dictionary<Guid, int>();
        await foreach (var comment in comments.All())
        {
            if (userPosts.AsMapped(p => p.PostId).Contains(comment.PostId))
            {
                commentCounts.TryGetValue(comment.PostId, out var count);
                commentCounts[comment.PostId] = count + 1;
            }
        }

        var author = await users.Load(query.UserId);
        var feed   = userPosts.OrderByDescending(p => p.CreatedAt).Take(query.Limit);

        foreach (var post in feed)
        {
            commentCounts.TryGetValue(post.PostId, out var commentCount);
            yield return new UserFeedProjection(
                PostId:       post.PostId,
                AuthorName:   author.Username,
                Content:      post.Content,
                LikeCount:    post.LikeCount,
                CommentCount: commentCount,
                CreatedAt:    post.CreatedAt
            );
        }
    }
}
