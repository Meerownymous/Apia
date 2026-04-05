namespace Apia.Tests.Record;

// Domain/Records/PostRecord.cs
public sealed record PostRecord(
    Guid PostId,
    Guid AuthorId,
    string Content,
    int LikeCount,
    IReadOnlySet<Guid> LikedByUserIds,
    DateTime CreatedAt
);