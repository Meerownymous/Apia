using Apia;
using Apia.Tests.Record;

namespace Apia.Tests.Examples.Userfeed;

/// <summary>The user's personal feed — posts by the user, most recent first, with comment counts.</summary>
public sealed class UserFeedProjection : IAggregateSource<UserPostSummaryView, UserFeedQuery>
{
    public async IAsyncEnumerable<UserPostSummaryView> From(IQuery<UserFeedQuery> query, IMemory memory)
    {
        var q        = query.Seed();
        var posts    = memory.Aggregate<PostRecord>();
        var comments = memory.Aggregate<CommentRecord>();
        var vault    = memory.Vault<UserRecord>();

        var userPosts = new List<PostRecord>();
        await foreach (var post in posts.From(new AllOf<PostRecord>()))
            if (post.AuthorId == q.UserId)
                userPosts.Add(post);

        var commentCounts = new Dictionary<Guid, int>();
        var postIds       = userPosts.Select(p => p.PostId).ToHashSet();
        await foreach (var comment in comments.From(new AllOf<CommentRecord>()))
            if (postIds.Contains(comment.PostId))
                commentCounts[comment.PostId] = commentCounts.GetValueOrDefault(comment.PostId) + 1;

        var userResult = await vault.Load(q.UserId);
        if (userResult.IsT1) yield break;
        var author = userResult.AsT0;

        foreach (var post in userPosts.OrderByDescending(p => p.CreatedAt).Take(q.Limit))
        {
            commentCounts.TryGetValue(post.PostId, out var commentCount);
            yield return new UserPostSummaryView(
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
