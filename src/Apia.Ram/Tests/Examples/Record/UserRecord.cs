namespace Apia.Ram.Tests.Examples.Record;

// Domain/Records/UserRecord.cs
public sealed record UserRecord(
    Guid UserId,
    string Username,
    Address Address = default  
);

public record Address
{
    public string City { get; set; } = string.Empty;
}