using Apia.Tests.Entities;

namespace Apia.Tests.Query;

/// <summary>
/// A backend's own answer to <see cref="UserFeed"/>: the author's posts are picked out by a condition
/// the backend evaluates, standing in for the single statement a storage technique would write by
/// hand. It answers the user and the limit the query names, so the same question gets the same answer
/// either way.
/// </summary>
public sealed class MatchedUserFeed : IAggregateOverride<UserFeed, UserPostSummary>
{
    public async IAsyncEnumerable<UserPostSummary> Results(UserFeed query, IMemory memory)
    {
        await foreach (var summary in
            (await memory.Vault<User>().Entity(query.UserId)).Match(
                author => Summaries(author, query, memory),
                _ => AsyncEnumerable.Empty<UserPostSummary>()))
            yield return summary;
    }

    private static async IAsyncEnumerable<UserPostSummary> Summaries(User author, UserFeed query, IMemory memory)
    {
        var posts = await memory.Vault<Post>().Matching(post => post.AuthorId == query.UserId).ToListAsync();
        var comments = await memory.Vault<Comment>().All().ToListAsync();
        foreach (var post in posts.OrderByDescending(post => post.CreatedAt).Take(query.Limit))
            yield return new UserPostSummary(
                PostId: post.PostId,
                AuthorName: author.Username,
                Content: post.Content,
                LikeCount: post.LikeCount,
                CommentCount: comments.Count(comment => comment.PostId == post.PostId),
                CreatedAt: post.CreatedAt);
    }
}
