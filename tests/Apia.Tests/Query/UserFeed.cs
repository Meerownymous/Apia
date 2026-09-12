using Apia.Tests.Entities;

namespace Apia.Tests.Query;

/// <summary>The user's personal feed — posts by the user, most recent first, with comment counts.</summary>
public sealed class UserFeed(Guid userId, int limit) : IAggregateQuery<UserPostSummary>
{
    public async IAsyncEnumerable<UserPostSummary> Results(IMemory memory)
    {
        await foreach (var summary in
            (await memory.Vault<User>().Entity(userId)).Match(
                author => Summaries(author, memory),
                _ => AsyncEnumerable.Empty<UserPostSummary>()))
            yield return summary;
    }

    private async IAsyncEnumerable<UserPostSummary> Summaries(User author, IMemory memory)
    {
        var posts = await memory.Vault<Post>().All().Where(post => post.AuthorId == userId).ToListAsync();
        var comments = await memory.Vault<Comment>().All().ToListAsync();
        foreach (var post in posts.OrderByDescending(post => post.CreatedAt).Take(limit))
            yield return new UserPostSummary(
                PostId: post.PostId,
                AuthorName: author.Username,
                Content: post.Content,
                LikeCount: post.LikeCount,
                CommentCount: comments.Count(comment => comment.PostId == post.PostId),
                CreatedAt: post.CreatedAt);
    }
}
