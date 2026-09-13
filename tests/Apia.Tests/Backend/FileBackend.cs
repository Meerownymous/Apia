using Apia.File;
using Apia.Tests.Identity;

namespace Apia.Tests.Backend;

/// <summary>The file backend, each memory under a directory of its own.</summary>
public sealed class FileBackend : IBackend
{
    public IMemory Memory() => Memory(new Overrides());

    public IMemory Memory(IOverrides overrides)
        => new FileMemory(
            Path.Combine(Path.GetTempPath(), $"apia-{Guid.NewGuid():N}"),
            new ExampleIdentities(),
            overrides);
}
