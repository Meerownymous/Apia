using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Ram;

public sealed class RamMemoryMapTests
{
    [Fact]
    public void RegistersVault()
    {
        var map   = new RamMemoryMap();
        var vault = new RamVault<UserRecord>();
        map.Register(vault);

        Assert.Equal(
            vault,
            map.Build().Vault<UserRecord>()
        );
    }

    [Fact]
    public async Task RegistersEntities()
    {
        var map    = new RamMemoryMap();
        var user   = new UserRecord(Guid.NewGuid(), "Miro");
        map.Register(new RamEntities<UserRecord>(u => u.UserId, u => u.Username));
        var memory = map.Build();

        await memory.Entities<UserRecord>().Save(user);
        var result = await memory.Entities<UserRecord>().Load(user.UserId);

        Assert.True(result.IsT0);
        Assert.Equal(user, result.AsT0);
    }

    [Fact]
    public void RegistersViews()
    {
        var map = new RamMemoryMap();
        // map.Register<UserFeedProjection, UserFeedQuery>(new UserFeedSynopsis());
        // var views = map.Build().Views<UserFeedProjection, UserFeedQuery>();
    }
}
