namespace Apia.Tests.Records;

public sealed record UserPostSummary(
    Guid PostId,
    string AuthorName,
    string Content,
    int LikeCount,
    int CommentCount,
    DateTime CreatedAt
);
