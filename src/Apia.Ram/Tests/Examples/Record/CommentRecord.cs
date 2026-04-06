namespace Apia.Tests.Record;

public sealed record CommentRecord(
    Guid CommentId,
    Guid PostId,
    Guid AuthorId,
    string Text,
    DateTime CreatedAt
);