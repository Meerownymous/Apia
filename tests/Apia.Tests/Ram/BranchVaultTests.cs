using Apia;
using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Ram;

public sealed class BranchVaultTests
{
    [Fact]
    public async Task Vault_Load_ReturnsNotFound_WhenEmpty()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        Assert.True((await map.Build().Vault<UserRecord>().Load(Guid.NewGuid())).Match(_ => false, _ => true));
    }

    [Fact]
    public async Task Branch_SaveAndCommit_LoadReturnsFound()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        var memory = map.Build();
        var user   = new UserRecord("Miro");
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();
        Assert.True((await memory.Vault<UserRecord>().Load(user.UserId)).Match(_ => true, _ => false));
    }

    [Fact]
    public async Task Branch_SaveAndCommit_LoadReturnsCorrectEntity()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        var memory = map.Build();
        var user   = new UserRecord("Miro");
        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();
        Assert.Equal(user, (await memory.Vault<UserRecord>().Load(user.UserId)).Match(found => found, _ => throw new InvalidOperationException("NotFound")));
    }

    [Fact]
    public async Task Branch_SaveWithoutCommit_DoesNotPersist()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        var memory = map.Build();
        var user   = new UserRecord("Miro");
        await memory.Branch().Save(user);
        Assert.True((await memory.Vault<UserRecord>().Load(user.UserId)).Match(_ => false, _ => true));
    }

    [Fact]
    public async Task Branch_DeleteAndCommit_RemovesEntity()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        var memory = map.Build();
        var user   = new UserRecord("Miro");
        var b1     = memory.Branch();
        await b1.Save(user);
        await b1.Commit();
        var b2 = memory.Branch();
        await b2.Delete<UserRecord>(user.UserId);
        await b2.Commit();
        Assert.True((await memory.Vault<UserRecord>().Load(user.UserId)).Match(_ => false, _ => true));
    }

    [Fact]
    public async Task Branch_SaveUpserts_LoadReturnsFound()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        var memory = map.Build();
        var user   = new UserRecord("Miro");
        var b1     = memory.Branch();
        await b1.Save(user);
        await b1.Commit();
        var b2 = memory.Branch();
        await b2.Save(user with { Username = "Ralph" });
        await b2.Commit();
        Assert.True((await memory.Vault<UserRecord>().Load(user.UserId)).Match(_ => true, _ => false));
    }

    [Fact]
    public async Task Branch_SaveUpserts_LoadReturnsUpdatedUsername()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        var memory = map.Build();
        var user   = new UserRecord("Miro");
        var b1     = memory.Branch();
        await b1.Save(user);
        await b1.Commit();
        var b2 = memory.Branch();
        await b2.Save(user with { Username = "Ralph" });
        await b2.Commit();
        Assert.Equal("Ralph", (await memory.Vault<UserRecord>().Load(user.UserId)).Match(found => found.Username, _ => throw new InvalidOperationException("NotFound")));
    }

    [Fact]
    public async Task Aggregate_AllOf_StreamsAllEntities()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        var memory = map.Build();
        var user1  = new UserRecord("Miro");
        var user2  = new UserRecord("Ralph");
        var branch = memory.Branch();
        await branch.Save(user1);
        await branch.Save(user2);
        await branch.Commit();
        Assert.Equal(2, (await memory.Aggregate<UserRecord>().From(new AllOf<UserRecord>()).ToListAsync()).Count);
    }
}
