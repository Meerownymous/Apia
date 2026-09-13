namespace Apia.Tests.Query;

/// <summary>
/// A backend's own answer to <see cref="UserFeed"/>, standing in for a statement that reads nothing but
/// the question: one entry per post the limit allows, each naming the user that was asked about.
/// </summary>
public sealed class HandWrittenUserFeed : IAggregateOverride<UserFeed, UserPostSummary>
{
    public IAsyncEnumerable<UserPostSummary> Results(UserFeed query, IMemory memory)
        => Enumerable
            .Range(0, query.Limit)
            .Select(_ => new UserPostSummary(
                PostId: Guid.Empty,
                AuthorName: query.UserId.ToString(),
                Content: string.Empty,
                LikeCount: 0,
                CommentCount: 0,
                CreatedAt: DateTime.UnixEpoch))
            .ToAsyncEnumerable();
}
