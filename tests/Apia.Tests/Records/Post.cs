namespace Apia.Tests.Records;

public sealed record Post(
    Guid PostId,
    Guid AuthorId,
    string Content,
    int LikeCount,
    DateTime CreatedAt
);
