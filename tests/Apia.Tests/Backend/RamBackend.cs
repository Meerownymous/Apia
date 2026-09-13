using Apia.Ram;
using Apia.Tests.Identity;

namespace Apia.Tests.Backend;

/// <summary>The in-memory backend.</summary>
public sealed class RamBackend : IBackend
{
    public IMemory Memory() => Memory(new Overrides());

    public IMemory Memory(IOverrides overrides) => new RamMemory(new ExampleIdentities(), overrides);
}
