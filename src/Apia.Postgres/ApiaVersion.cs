namespace Apia.Postgres;

/// <summary>
/// Internal version tracking document stored in Marten.
/// One record per (RecordType, RecordId) pair.
/// </summary>
public sealed record ApiaVersion(
    Guid Id,
    string RecordType,
    Guid RecordId,
    uint Version
);
