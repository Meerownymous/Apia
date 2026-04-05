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
    public void RegistersEntities()
    {
        var map      = new RamMemoryMap();
        var entities = new RamEntities<UserRecord>(u => u.UserId, u => u.Username);
        map.Register(entities);

        Assert.Equal(
            entities,
            map.Build().Entities<UserRecord>()
        );
    }

    [Fact]
    public void RegistersViews()
    {
        var map = new RamMemoryMap();
        // map.Register<UserFeedProjection, UserFeedQuery>(new UserFeedSynopsis());
        // var views = map.Build().Views<UserFeedProjection, UserFeedQuery>();
    }
}
