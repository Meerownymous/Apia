using System.Reflection;
using Apia.File;
using Apia.Postgres;
using Apia.Ram;
using Xunit;

namespace Apia.Tests.Query;

/// <summary>Every assembly the library publishes: the core and the three backends.</summary>
public sealed class ApiaAssemblies : TheoryData<Assembly>
{
    public ApiaAssemblies()
    {
        Add(typeof(IMemory).Assembly);
        Add(typeof(RamMemory).Assembly);
        Add(typeof(FileMemory).Assembly);
        Add(typeof(PostgresMemory).Assembly);
    }
}
