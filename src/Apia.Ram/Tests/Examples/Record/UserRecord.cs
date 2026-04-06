namespace Apia.Tests.Record;

// Domain/Records/UserRecord.cs
public sealed record UserRecord(
    Guid UserId,
    string Username
);