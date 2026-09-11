namespace Apia.Tests.Records;

public sealed record Comment(
    Guid CommentId,
    Guid PostId,
    Guid AuthorId,
    string Text,
    DateTime CreatedAt
);
