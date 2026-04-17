using Apia;
using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Ram;

public sealed class BranchVaultTests
{
    private static IMemory BuildMemory()
    {
        var map = new RamMemoryMap();
        map.RegisterStore(new UserRecordId());
        return map.Build();
    }

    [Fact]
    public async Task Vault_Load_ReturnsNotFound_WhenEmpty()
    {
        var memory = BuildMemory();

        var result = await memory.Vault<UserRecord>().Load(Guid.NewGuid());

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task Branch_SaveAndCommit_LoadReturnsT0()
    {
        var memory = BuildMemory();
        var user   = new UserRecord("Miro");

        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();

        var result = await memory.Vault<UserRecord>().Load(user.UserId);
        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task Branch_SaveAndCommit_LoadReturnsCorrectEntity()
    {
        var memory = BuildMemory();
        var user   = new UserRecord("Miro");

        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();

        var result = await memory.Vault<UserRecord>().Load(user.UserId);
        Assert.Equal(user, result.AsT0);
    }

    [Fact]
    public async Task Branch_SaveWithoutCommit_DoesNotPersist()
    {
        var memory = BuildMemory();
        var user   = new UserRecord("Miro");

        var branch = memory.Branch();
        await branch.Save(user);
        // no Commit()

        var result = await memory.Vault<UserRecord>().Load(user.UserId);
        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task Branch_DeleteAndCommit_RemovesEntity()
    {
        var memory = BuildMemory();
        var user   = new UserRecord("Miro");

        var branch = memory.Branch();
        await branch.Save(user);
        await branch.Commit();

        var branch2 = memory.Branch();
        await branch2.Delete<UserRecord>(user.UserId);
        await branch2.Commit();

        var result = await memory.Vault<UserRecord>().Load(user.UserId);
        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task Branch_SaveUpserts_LoadReturnsT0()
    {
        var memory = BuildMemory();
        var user   = new UserRecord("Miro");

        var b1 = memory.Branch();
        await b1.Save(user);
        await b1.Commit();

        var updated = user with { Username = "Ralph" };
        var b2      = memory.Branch();
        await b2.Save(updated);
        await b2.Commit();

        var result = await memory.Vault<UserRecord>().Load(user.UserId);
        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task Branch_SaveUpserts_LoadReturnsUpdatedUsername()
    {
        var memory = BuildMemory();
        var user   = new UserRecord("Miro");

        var b1 = memory.Branch();
        await b1.Save(user);
        await b1.Commit();

        var updated = user with { Username = "Ralph" };
        var b2      = memory.Branch();
        await b2.Save(updated);
        await b2.Commit();

        var result = await memory.Vault<UserRecord>().Load(user.UserId);
        Assert.Equal("Ralph", result.AsT0.Username);
    }

    [Fact]
    public async Task Aggregate_AllOf_StreamsAllEntities()
    {
        var memory = BuildMemory();
        var user1  = new UserRecord("Miro");
        var user2  = new UserRecord("Ralph");

        var branch = memory.Branch();
        await branch.Save(user1);
        await branch.Save(user2);
        await branch.Commit();

        var all = await memory.Aggregate<UserRecord>()
            .From(new AllOf<UserRecord>())
            .ToListAsync();

        Assert.Equal(2, all.Count);
    }
}
