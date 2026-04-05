using Apia.Ram;
using Apia.Tests.Record;
using Xunit;

namespace Apia.Tests.Ram;

public sealed class RamMemoryMapTests
{
    [Fact]
    public void RegistersMutable()
    {
        var map = new RamMemoryMap();
        var mutable = new RamMutable<UserRecord>();
        map.Register(mutable);
        
        Assert.Equal(
            mutable,
            map.Build().Mutable<UserRecord>()
        );
    }
    
    [Fact]
    public void RegistersMutableCatalog()
    {
        var map = new RamMemoryMap();
        var catalog = new RamMutableCatalog<UserRecord>(u => u.UserId, u => u.Username);
        map.Register(catalog);
        
        Assert.Equal(
            catalog,
            map.Build().Catalog<UserRecord>()
        );
    }
    
    [Fact]
    public void RegistersSynopsis()
    {
        var map = new RamMemoryMap();
        //bar synopsis = new Ram
        // map.Register(catalog);
        //
        // Assert.Equal(
        //     catalog,
        //     map.Build().Catalog<UserRecord>()
        // );
    }
}