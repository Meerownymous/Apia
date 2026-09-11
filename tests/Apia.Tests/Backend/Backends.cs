using Xunit;

namespace Apia.Tests.Backend;

/// <summary>Every backend the contract suite runs against, unreachable ones included so they are named.</summary>
public sealed class Backends : TheoryData<IBackend>
{
    public Backends()
    {
        Add(new RamBackend());
        Add(new FileBackend());
        Add(Postgres(Environment.GetEnvironmentVariable("APIA_POSTGRES_CONNECTION") ?? string.Empty));
    }

    private static IBackend Postgres(string connection)
        => connection.Length == 0
            ? new UnreachablePostgres(
                "APIA_POSTGRES_CONNECTION is not set, so no database was available to run against")
            : new PostgresBackend(connection);
}
