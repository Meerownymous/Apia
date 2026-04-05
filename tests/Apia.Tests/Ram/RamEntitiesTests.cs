using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Ram;

public sealed class RamEntitiesTests
{
    [Fact]
    public async Task Load_ReturnsNotFound_WhenEmpty()
    {
        var entities = new RamEntities<UserRecord>(u => u.UserId);

        var result = await entities.Load(Guid.NewGuid());

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task Load_ReturnsRecord_AfterSave()
    {
        var entities = new RamEntities<UserRecord>(u => u.UserId);
        var user = new UserRecord(Guid.NewGuid(), "Miro");
        await entities.Save(user);

        var result = await entities.Load(user.UserId);

        Assert.True(result.IsT0);
        Assert.Equal(user, result);
    }

    [Fact]
    public async Task Save_ReturnsRecord_OnSuccess()
    {
        var entities = new RamEntities<UserRecord>(u => u.UserId);
        var user = new UserRecord(Guid.NewGuid(), "Miro");

        var result = await entities.Save(user);

        Assert.True(result.IsT0);
        Assert.Equal(user, result.AsT0);
    }

    [Fact]
    public async Task Save_ReturnsConflict_WhenModifiedBetweenLoadAndSave()
    {
        var entities = new RamEntities<UserRecord>(u => u.UserId);
        var user = new UserRecord(Guid.NewGuid(), "Miro");
        await entities.Save(user);

        // First caller loads
        await entities.Load(user.UserId);

        // Second caller saves before first caller does
        await entities.Save(user with { Username = "Ralph" });

        // First caller tries to save — should conflict
        var result = await entities.Save(user with { Username = "Hans" });

        Assert.True(result.IsT1);
        Assert.Equal("Ralph",  result.AsT1.Current.Username);
        Assert.Equal("Hans",   result.AsT1.Attempted.Username);
    }

    [Fact]
    public async Task Save_Succeeds_AfterRetryWithFreshLoad()
    {
        var entities = new RamEntities<UserRecord>(u => u.UserId);
        var user = new UserRecord(Guid.NewGuid(), "Miro");
        await entities.Save(user);

        // Concurrent save by someone else
        await entities.Load(user.UserId);
        await entities.Save(user with { Username = "Ralph" });

        // Retry: load fresh, then save
        var loaded = await entities.Load(user.UserId);
        var result = await entities.Save(loaded.AsT0 with { Username = "Hans" });

        Assert.True(result.IsT0);
        Assert.Equal("Hans", result.AsT0.Username);
    }

    [Fact]
    public async Task Save_NoConflict_OnFirstInsert()
    {
        var entities = new RamEntities<UserRecord>(u => u.UserId);
        var user = new UserRecord(Guid.NewGuid(), "Miro");

        // Save without prior Load — first insert, no conflict expected
        var result = await entities.Save(user);

        Assert.True(result.IsT0);
    }

    [Fact]
    public async Task Save_ReturnsConflict_OnSecondInsertWithoutLoad()
    {
        var entities = new RamEntities<UserRecord>(u => u.UserId);
        var user = new UserRecord(Guid.NewGuid(), "Miro");
        await entities.Save(user);

        // Save again without loading — version mismatch
        var result = await entities.Save(user with { Username = "Ralph" });

        Assert.True(result.IsT1);
        Assert.Equal("Miro",  result.AsT1.Current.Username);
        Assert.Equal("Ralph", result.AsT1.Attempted.Username);
    }
}
