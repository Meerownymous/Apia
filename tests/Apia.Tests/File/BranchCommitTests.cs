using Apia.File;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.File;

public sealed class BranchCommitTests
{
    [Fact]
    public async Task Branch_Commit_Throws_WhenEntityCannotBeSerialised()
    {
        var map = new FileMemoryMap(Path.Combine(Path.GetTempPath(), $"apia-{Guid.NewGuid():N}"));
        map.RegisterStore(new MeasurementRecordId());
        var branch = map.Build().Branch();
        await branch.Save(new MeasurementRecord(Guid.NewGuid(), double.NaN));
        await Assert.ThrowsAnyAsync<ArgumentException>(branch.Commit);
    }

    [Fact]
    public async Task Vault_Load_ReturnsEarlierEntity_WhenLaterCommitFails()
    {
        var map = new FileMemoryMap(Path.Combine(Path.GetTempPath(), $"apia-{Guid.NewGuid():N}"));
        map.RegisterStore(new MeasurementRecordId());
        var memory   = map.Build();
        var measured = new MeasurementRecord(Guid.NewGuid(), 21.5);
        var earlier  = memory.Branch();
        await earlier.Save(measured);
        await earlier.Commit();
        var later = memory.Branch();
        await later.Save(new MeasurementRecord(Guid.NewGuid(), double.NaN));
        try { await later.Commit(); } catch (ArgumentException) { }
        Assert.Equal(measured, (await memory.Vault<MeasurementRecord>().Load(measured.MeasurementId)).Match(found => found, _ => throw new InvalidOperationException("NotFound")));
    }
}
