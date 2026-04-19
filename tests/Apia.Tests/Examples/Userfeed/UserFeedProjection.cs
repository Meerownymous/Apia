using Apia;
using Apia.Tests.Record;

namespace Apia.Tests.Examples.Userfeed;

/// <summary>The user's personal feed — posts by the user, most recent first, with comment counts.</summary>
public sealed class UserFeedProjection : IAggregateSource<UserPostSummaryView, UserFeedQuery>
{
    public async IAsyncEnumerable<UserPostSummaryView> From(IQuery<UserFeedQuery> query, IMemory memory)
    {
        var q = query.Seed();
        await foreach (var view in
            (await memory.Vault<UserRecord>().Load(q.UserId.ToString())).Match(
                author => FeedViews(q, author, memory),
                _ => AsyncEnumerable.Empty<UserPostSummaryView>()))
            yield return view;
    }

    private static async IAsyncEnumerable<UserPostSummaryView> FeedViews(
        UserFeedQuery q, UserRecord author, IMemory memory)
    {
        var userPosts = new List<PostRecord>();
        await foreach (var post in memory.Aggregate<PostRecord>(new AllOf<PostRecord>()))
            if (post.AuthorId == q.UserId)
                userPosts.Add(post);

        var commentCounts = new Dictionary<Guid, int>();
        var postIds = userPosts.Select(p => p.PostId).ToHashSet();
        await foreach (var comment in memory.Aggregate<CommentRecord>(new AllOf<CommentRecord>()))
            if (postIds.Contains(comment.PostId))
                commentCounts[comment.PostId] = commentCounts.GetValueOrDefault(comment.PostId) + 1;

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
