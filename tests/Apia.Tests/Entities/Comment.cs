namespace Apia.Tests.Entities;

public sealed record Comment(
    Guid CommentId,
    Guid PostId,
    Guid AuthorId,
    string Text,
    DateTime CreatedAt
);
