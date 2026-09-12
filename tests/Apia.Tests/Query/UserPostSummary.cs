namespace Apia.Tests.Query;

/// <summary>One entry of a <see cref="UserFeed"/>: a post as the feed presents it, never stored.</summary>
public sealed record UserPostSummary(
    Guid PostId,
    string AuthorName,
    string Content,
    int LikeCount,
    int CommentCount,
    DateTime CreatedAt
);
