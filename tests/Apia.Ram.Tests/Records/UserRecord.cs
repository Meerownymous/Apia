using Spectre.Console.Rendering;

namespace Apia.Tests.Record;

// Domain/Records/UserRecord.cs
public sealed record UserRecord(
    Guid UserId,
    string Username
)
{
    public UserRecord(string username) : this(Guid.NewGuid(), username) { }
}