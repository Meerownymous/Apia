using Apia.File;
using Apia.Tests.Entities;
using Apia.Tests.Identity;
using Xunit;

namespace Apia.Tests.File;

/// <summary>
/// The file backend's own promise: a write that fails costs nothing rather than everything of that
/// type. A serialisation failure once emptied the type's file before it ever wrote into it.
/// </summary>
public sealed class FileWriteTests
{
    [Fact]
    public async Task Commit_Throws_WhenAnEntityCannotBeSerialised()
    {
        var memory = new FileMemory(
            Path.Combine(Path.GetTempPath(), $"apia-{Guid.NewGuid():N}"),
            new ExampleIdentities(),
            new Overrides());
        var branch = memory.Branch();
        await branch.Save(new Measurement(Guid.NewGuid(), double.NaN));

        await Assert.ThrowsAnyAsync<ArgumentException>(async () => await branch.Commit());
    }

    [Fact]
    public async Task Vault_Entity_ReturnsTheEarlierEntity_WhenALaterWriteFailed()
    {
        var memory = new FileMemory(
            Path.Combine(Path.GetTempPath(), $"apia-{Guid.NewGuid():N}"),
            new ExampleIdentities(),
            new Overrides());
        var measured = new Measurement(Guid.NewGuid(), 21.5);
        var earlier = memory.Branch();
        await earlier.Save(measured);
        await earlier.Commit();
        var later = memory.Branch();
        await later.Save(new Measurement(Guid.NewGuid(), double.NaN));
        try { await later.Commit(); } catch (ArgumentException) { }

        Assert.Equal(
            measured,
            (await memory.Vault<Measurement>().Entity(measured.MeasurementId))
                .Match(found => found, _ => throw new InvalidOperationException("NotFound")));
    }
}
