using Apia.Tests.Record;
using Tonga.Enumerable;

namespace Apia.Ram.Tests.Examples.UseCases.Userfeed;


public sealed class UserFeedSynopsis() : RamSynopsisStreamTmp<UserPostSummaryProjection>(Query)
{
    private static async IAsyncEnumerable<UserPostSummaryProjection> Query(IMemoryTmp memory, IQuery<UserPostSummaryProjection> query)
    {
        var posts    = memory.Entities<PostRecord>();
        var comments = memory.Entities<CommentRecord>();
        var users    = memory.Entities<UserRecord>();

        var userPosts = new List<PostRecord>();
        await foreach (var post in posts.All())
        {
            if (post.AuthorId == queryRecord.UserId)
                userPosts.Add(post);
        }

        var commentCounts = new Dictionary<Guid, int>();
        await foreach (var comment in comments.Find())
        {
            if (userPosts.AsMapped(p => p.PostId).Contains(comment.PostId))
            {
                commentCounts.TryGetValue(comment.PostId, out var count);
                commentCounts[comment.PostId] = count + 1;
            }
        }

        var userResult = await users.Load(queryRecord.UserId);
        if (userResult.IsT1) yield break;
        var author = userResult.AsT0;
        var feed   = userPosts.OrderByDescending(p => p.CreatedAt).Take(queryRecord.Limit);

        foreach (var post in feed)
        {
            commentCounts.TryGetValue(post.PostId, out var commentCount);
            yield return new UserPostSummaryProjection(
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
