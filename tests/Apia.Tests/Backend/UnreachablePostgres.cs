using Xunit;

namespace Apia.Tests.Backend;

/// <summary>
/// Postgres where this run cannot reach a database. Asking it for a memory skips the test with the
/// reason, so that a green run without it cannot be mistaken for full coverage.
/// </summary>
public sealed class UnreachablePostgres(string reason) : IBackend
{
    public IMemory Memory() => throw new SkipException($"Postgres was not exercised: {reason}");
}
