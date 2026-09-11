using Apia.Ram;
using Apia.Tests.Identity;

namespace Apia.Tests.Backend;

/// <summary>The in-memory backend.</summary>
public sealed class RamBackend : IBackend
{
    public IMemory Memory() => new RamMemory(new ExampleIdentities(), new Overrides());
}
