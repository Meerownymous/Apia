using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>The in-memory stores of one memory, one per entity type, created when a type is first touched.</summary>
public sealed class RamStores(IIdentities identities) : IStores
{
    private readonly ConcurrentDictionary<Type, object> stores = new();

    public IEntityStore<T> Store<T>() where T : notnull
        => (IEntityStore<T>)stores.GetOrAdd(typeof(T), _ => new RamEntityStore<T>(new GivenIdentity<T>(identities)));
}
