namespace Apia.Tests.Record;

public sealed record UserPostSummaryProjection(
    Guid PostId,
    string AuthorName,
    string Content,
    int LikeCount,
    int CommentCount,
    DateTime CreatedAt
);